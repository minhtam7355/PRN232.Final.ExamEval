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
            _logger?.LogInformation("Found solution {Sln}", slnPath);

            var workingDir = Path.GetDirectoryName(slnPath) ?? extractedRoot;
            var outputBuilder = new StringBuilder();

            // Step 1: Clean first to avoid cache issues
            _logger?.LogInformation("Cleaning solution {Sln}", slnPath);
            var cleanResult = await RunDotnetCommand("clean", slnPath, workingDir, outputBuilder, ct);
            if (!cleanResult)
            {
                _logger?.LogWarning("Clean failed, but continuing with build");
            }

            // Step 2: Delete bin/obj folders manually to ensure clean state
            try
            {
                var binObjDirs = Directory.GetDirectories(workingDir, "bin", SearchOption.AllDirectories)
                    .Concat(Directory.GetDirectories(workingDir, "obj", SearchOption.AllDirectories));
                
                foreach (var dir in binObjDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        _logger?.LogDebug("Deleted {Dir}", dir);
                    }
                    catch
                    {
                        // Ignore individual folder delete errors
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clean bin/obj folders");
            }

            // Step 3: Restore NuGet packages with --force and --no-cache
            _logger?.LogInformation("Restoring NuGet packages for {Sln}", slnPath);
            var restoreResult = await RunDotnetCommandWithArgs(
                "restore", 
                $"\"{slnPath}\" --force --no-cache", 
                workingDir, 
                outputBuilder, 
                ct);
            
            if (!restoreResult)
            {
                var output = outputBuilder.ToString();
                _logger?.LogError("NuGet restore failed for {Sln}", slnPath);
                return new BuildResult(false, $"NuGet restore failed:\n{output}");
            }

            // Step 4: Build with --no-restore to use the packages we just restored
            _logger?.LogInformation("Building solution {Sln}", slnPath);
            var buildResult = await RunDotnetCommandWithArgs(
                "build", 
                $"\"{slnPath}\" -c Release --no-restore", 
                workingDir, 
                outputBuilder, 
                ct);

            var finalOutput = outputBuilder.ToString();
            
            if (buildResult)
            {
                _logger?.LogInformation("✅ Build succeeded for {Sln}", slnPath);
            }
            else
            {
                _logger?.LogError("❌ Build failed for {Sln}", slnPath);
            }

            return new BuildResult(buildResult, finalOutput);
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
            return await RunDotnetCommandWithArgs(command, $"\"{slnPath}\"", workingDir, outputBuilder, ct);
        }

        /// <summary>
        /// Run a dotnet command with custom arguments and capture output
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

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

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
                _logger?.LogDebug("dotnet {Command} exited with code {Code}", command, exitCode);

                return exitCode == 0;
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
