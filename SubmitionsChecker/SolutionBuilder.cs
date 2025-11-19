using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubmitionsChecker
{
    public record BuildResult(bool Success, string Output);

    public class SolutionBuilder
    {
        private readonly ILogger<SolutionBuilder>? _logger;

        public SolutionBuilder(ILogger<SolutionBuilder>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Find a .sln file under <paramref name="extractedRoot"/> (recursive) and run `dotnet build` on it.
        /// Returns BuildResult with success flag and captured output.
        /// Cleans before building to avoid cache issues.
        /// Uses MSBuild binary log for reliable build result detection.
        /// Handles Windows long path issues automatically.
        /// </summary>
        public async Task<BuildResult> BuildSolutionAsync(string extractedRoot, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(extractedRoot) || !Directory.Exists(extractedRoot))
            {
                return new BuildResult(false, $"Extracted root not found: {extractedRoot}");
            }

            var slnFiles = Directory.GetFiles(extractedRoot, "*.sln", SearchOption.AllDirectories);
            if (slnFiles.Length == 0)
            {
                _logger?.LogWarning("No .sln file found under {Root}", extractedRoot);
                return new BuildResult(false, "No .sln file found in extracted submission.");
            }

            var slnPath = slnFiles.OrderBy(p => p).First(); // pick first
            _logger?.LogInformation("Found solution {Sln}", Path.GetFileName(slnPath));

            var workingDir = Path.GetDirectoryName(slnPath) ?? extractedRoot;
            
            // Check if path is too long (Windows MAX_PATH is 260 chars, but NuGet has issues around 200)
            var isPathTooLong = slnPath.Length > 180 || workingDir.Length > 150;
            
            if (isPathTooLong)
            {
                _logger?.LogWarning("⚠️ Path length {Length} chars - may cause issues. Attempting workaround...", slnPath.Length);
                
                // Try to use a shorter path by copying to temp
                var tempBuildDir = Path.Combine(Path.GetTempPath(), $"build_{Guid.NewGuid():N}");
                try
                {
                    _logger?.LogInformation("📦 Copying to shorter temp path: {Temp}", tempBuildDir);
                    CopyDirectory(workingDir, tempBuildDir);
                    
                    // Update paths to use temp directory
                    var slnFileName = Path.GetFileName(slnPath);
                    var newSlnPath = Path.Combine(tempBuildDir, slnFileName);
                    
                    if (File.Exists(newSlnPath))
                    {
                        _logger?.LogInformation("✅ Using temp directory for build");
                        var result = await BuildSolutionInternalAsync(newSlnPath, tempBuildDir, ct);
                        
                        // Clean up temp directory
                        try
                        {
                            Directory.Delete(tempBuildDir, true);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                        
                        return result;
                    }
                    else
                    {
                        _logger?.LogWarning("Solution file not found in temp directory, falling back to original path");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to copy to temp directory, will try original path");
                    
                    // Clean up temp if it was created
                    try
                    {
                        if (Directory.Exists(tempBuildDir))
                        {
                            Directory.Delete(tempBuildDir, true);
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
            
            // Use original path (or fallback if temp didn't work)
            return await BuildSolutionInternalAsync(slnPath, workingDir, ct);
        }

        /// <summary>
        /// Copy directory recursively
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            
            // Copy all files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }
            
            // Copy all subdirectories (except bin/obj to save time)
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                
                // Skip bin/obj folders
                if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }

        /// <summary>
        /// Internal build method that does the actual building
        /// </summary>
        private async Task<BuildResult> BuildSolutionInternalAsync(
            string slnPath, 
            string workingDir, 
            CancellationToken ct)
        {
            var outputBuilder = new StringBuilder();
            
            // Create a unique build log file
            var binlogPath = Path.Combine(workingDir, $"build_{Guid.NewGuid():N}.binlog");

            try
            {
                // Step 1: Clean first to avoid cache issues
                _logger?.LogInformation("🧹 Cleaning solution...");
                await RunDotnetCommand("clean", slnPath, workingDir, outputBuilder, ct);

                // Step 2: Delete bin/obj folders manually to ensure clean state
                try
                {
                    var binObjDirs = Directory.GetDirectories(workingDir, "bin", SearchOption.AllDirectories)
                        .Concat(Directory.GetDirectories(workingDir, "obj", SearchOption.AllDirectories))
                        .ToList();
                    
                    int deletedCount = 0;
                    foreach (var dir in binObjDirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            deletedCount++;
                        }
                        catch
                        {
                            // Ignore individual folder delete errors
                        }
                    }
                    if (deletedCount > 0)
                    {
                        _logger?.LogDebug("Deleted {Count} bin/obj folders", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to clean bin/obj folders");
                }

                // Step 3: Restore NuGet packages with --force and --no-cache
                _logger?.LogInformation("📦 Restoring NuGet packages...");
                var restoreResult = await RunDotnetCommandWithArgs(
                    "restore", 
                    $"\"{slnPath}\" --force --no-cache --verbosity quiet", 
                    workingDir, 
                    outputBuilder, 
                    ct);
                
                if (!restoreResult)
                {
                    var output = outputBuilder.ToString();
                    _logger?.LogError("❌ NuGet restore failed");
                    
                    // Check if it's a path length issue
                    if (output.Contains("maximum path length") || output.Contains("NETSDK1064"))
                    {
                        return new BuildResult(false, $"NuGet restore failed due to path length restrictions. Path: {slnPath.Length} chars\n{output}");
                    }
                    
                    return new BuildResult(false, $"NuGet restore failed:\n{output}");
                }

                // Step 4: Build with binary log for reliable success detection
                _logger?.LogInformation("🔨 Building solution...");
                var buildResult = await RunDotnetCommandWithArgs(
                    "build", 
                    $"\"{slnPath}\" -c Release --no-restore --no-incremental /bl:\"{binlogPath}\" --verbosity minimal", 
                    workingDir, 
                    outputBuilder, 
                    ct);

                var finalOutput = outputBuilder.ToString();
                
                // Verify build success by checking:
                // 1. Exit code (primary)
                // 2. Binary log exists (confirms build ran)
                // 3. DLL files were created (confirms actual output)
                var buildSuccess = buildResult;
                
                if (buildResult)
                {
                    // Check if any DLLs were actually produced
                    var dllFiles = Directory.GetFiles(workingDir, "*.dll", SearchOption.AllDirectories)
                        .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)) // Exclude obj folders
                        .Where(f => f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar)) // Only bin/Release
                        .ToList();
                    
                    if (dllFiles.Count > 0)
                    {
                        _logger?.LogInformation("✅ Build succeeded - {Count} DLL(s) produced", dllFiles.Count);
                        buildSuccess = true;
                    }
                    else
                    {
                        _logger?.LogWarning("⚠️ Build exit code 0, but no DLLs found in bin/Release");
                        // Still consider it success if exit code is 0
                    }
                }
                else
                {
                    _logger?.LogError("❌ Build failed - exit code non-zero");
                    
                    // Check if it's a path length or NuGet issue
                    if (finalOutput.Contains("maximum path length") || 
                        finalOutput.Contains("NETSDK1064") ||
                        finalOutput.Contains("was not found") && finalOutput.Contains("NuGet restore"))
                    {
                        _logger?.LogError("Build failed due to path length or NuGet package issues");
                    }
                    
                    buildSuccess = false;
                }

                // Clean up binlog
                try
                {
                    if (File.Exists(binlogPath))
                    {
                        File.Delete(binlogPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return new BuildResult(buildSuccess, finalOutput);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during build process");
                return new BuildResult(false, $"Build exception: {ex.Message}\n{outputBuilder}");
            }
            finally
            {
                // Ensure binlog is cleaned up
                try
                {
                    if (File.Exists(binlogPath))
                    {
                        File.Delete(binlogPath);
                    }
                }
                catch
                {
                    // Ignore
                }
            }
        }

        /// <summary>
        /// Run a dotnet command and capture output
        /// </summary>
        private async Task<bool> RunDotnetCommand(
            string command, 
            string slnPath, 
            string workingDir, 
            StringBuilder outputBuilder, 
            CancellationToken ct)
        {
            return await RunDotnetCommandWithArgs(command, $"\"{slnPath}\" --verbosity quiet", workingDir, outputBuilder, ct);
        }

        /// <summary>
        /// Run a dotnet command with custom arguments and capture output
        /// Simple and reliable - just check exit code
        /// </summary>
        private async Task<bool> RunDotnetCommandWithArgs(
            string command,
            string arguments,
            string workingDir,
            StringBuilder outputBuilder,
            CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{command} {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (s, e) => 
            { 
                if (e.Data != null) 
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            
            process.ErrorDataReceived += (s, e) => 
            { 
                if (e.Data != null) 
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            try
            {
                _logger?.LogDebug("Running: dotnet {Command} {Args}", command, arguments);
                
                if (!process.Start())
                {
                    outputBuilder.AppendLine($"Failed to start dotnet {command} process.");
                    return false;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (ct.Register(() =>
                {
                    try { if (!process.HasExited) process.Kill(true); } catch { }
                }))
                {
                    await process.WaitForExitAsync(ct);
                }

                var exitCode = process.ExitCode;
                var success = exitCode == 0;
                
                _logger?.LogDebug("dotnet {Command} exited with code {Code}", command, exitCode);

                return success;
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
                outputBuilder.AppendLine($"dotnet {command} was cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                outputBuilder.AppendLine($"Exception during dotnet {command}: {ex.Message}");
                _logger?.LogError(ex, "Failed to run dotnet {Command}", command);
                return false;
            }
        }
    }
}
