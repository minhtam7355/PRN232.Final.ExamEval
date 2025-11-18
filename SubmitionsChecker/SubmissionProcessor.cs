using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubmitionsChecker
{
    public enum ViolationType
    {
        MissingSolutionFile,
        WrongFolderName,
        MissingMainFile,
        BuildFailed,
        Invalid3LayerArchitecture
    }

    public record Violation(string StudentFolder, ViolationType Type, string Message);

    public record ProcessingProgress(int Total, int Completed, int Failed, string? CurrentStudent = null);

    public record StudentProgress(string StudentName, string Status, string? Error = null, List<string> Violations = null);

    public class SubmissionProcessor
    {
        private readonly ILogger<SubmissionProcessor>? _logger;
        private static readonly Regex StudentFolderRegex = new Regex(@"SE\d{6}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public SubmissionProcessor(ILogger<SubmissionProcessor>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process extracted submissions root directory. Each first-level folder is a student.
        /// Produces normalized text files under normalizedOutputDir and returns detected violations.
        /// Processes students in parallel with optional maxDegreeOfParallelism.
        /// </summary>
        public async Task<List<Violation>> ProcessAsync(
            string extractedRootDir, 
            string normalizedOutputDir, 
            int maxDegreeOfParallelism = 4, 
            Action<ProcessingProgress>? onProgress = null,
            Func<StudentProgress, Task>? onStudentProgress = null,
            CancellationToken ct = default)
        {
            _logger?.LogInformation("Start processing submissions from {Root} -> normalized into {Out}", extractedRootDir, normalizedOutputDir);

            var violations = new ConcurrentBag<Violation>();

            if (!Directory.Exists(extractedRootDir))
            {
                _logger?.LogError("Extracted root directory not found: {Root}", extractedRootDir);
                throw new DirectoryNotFoundException($"Extracted root directory not found: {extractedRootDir}");
            }

            Directory.CreateDirectory(normalizedOutputDir);

            // Find student directories - recursively search for folders that:
            // 1. Have a name matching SE + 6 digits pattern
            // 2. Contain a subfolder named "0" (which should contain solution.zip)
            var allDirs = Directory.GetDirectories(extractedRootDir, "*", SearchOption.AllDirectories);
            var studentDirs = allDirs
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    
                    // Skip system/normalized folders
                    if (string.Equals(name, "Normalized", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.StartsWith("_")) return false;
                    if (string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)) return false;
                    if (string.Equals(name, "solution_extracted", StringComparison.OrdinalIgnoreCase)) return false;
                    
                    // Check if folder name contains SE pattern (student ID)
                    if (!StudentFolderRegex.IsMatch(name)) return false;
                    
                    // Verify this is actually a student folder by checking if it has a "0" subfolder
                    var zeroDir = Path.Combine(d, "0");
                    return Directory.Exists(zeroDir);
                })
                .ToArray();

            _logger?.LogInformation("Found {Count} student directories (with '0' subfolder)", studentDirs.Length);

            int totalStudents = studentDirs.Length;
            int completed = 0;
            int failed = 0;

            onProgress?.Invoke(new ProcessingProgress(totalStudents, completed, failed));

            using var sem = new SemaphoreSlim(Math.Max(1, maxDegreeOfParallelism));
            var tasks = new List<Task>();

            foreach (var dir in studentDirs)
            {
                await sem.WaitAsync(ct);

                ct.ThrowIfCancellationRequested();

                tasks.Add(Task.Run(async () =>
                {
                    bool hasErrors = false;
                    var folderName = Path.GetFileName(dir) ?? dir;
                    var studentViolations = new List<string>();
                    
                    try
                    {
                        _logger?.LogDebug("Processing student folder: {Folder} at {Path}", folderName, dir);

                        // Notify student processing started
                        if (onStudentProgress != null)
                            await onStudentProgress(new StudentProgress(folderName, "processing"));

                        onProgress?.Invoke(new ProcessingProgress(totalStudents, completed, failed, folderName));

                        // Extract student ID from folder name using regex
                        var match = StudentFolderRegex.Match(folderName);
                        if (!match.Success)
                        {
                            _logger?.LogWarning("Folder name does not contain valid student ID: {Folder}", folderName);
                            var violation = new Violation(folderName, ViolationType.WrongFolderName, "Folder name must contain SE followed by 6 digits (e.g. SE123456)");
                            violations.Add(violation);
                            studentViolations.Add(violation.Message);
                            hasErrors = true;
                        }

                        var zeroDir = Path.Combine(dir, "0");
                        if (!Directory.Exists(zeroDir))
                        {
                            _logger?.LogWarning("Missing folder '0' for {Folder}", folderName);
                            var violation = new Violation(folderName, ViolationType.MissingSolutionFile, "Missing folder '0'");
                            violations.Add(violation);
                            studentViolations.Add(violation.Message);
                            hasErrors = true;
                            
                            if (onStudentProgress != null)
                                await onStudentProgress(new StudentProgress(folderName, "failed", "Missing folder '0'", studentViolations));
                            return;
                        }

                        var solutionZip = Path.Combine(zeroDir, "solution.zip");
                        if (!File.Exists(solutionZip))
                        {
                            _logger?.LogWarning("Missing solution.zip in 0/ for {Folder}", folderName);
                            var violation = new Violation(folderName, ViolationType.MissingSolutionFile, "Missing solution.zip in 0/");
                            violations.Add(violation);
                            studentViolations.Add(violation.Message);
                            hasErrors = true;
                            
                            if (onStudentProgress != null)
                                await onStudentProgress(new StudentProgress(folderName, "failed", "Missing solution.zip", studentViolations));
                            return;
                        }

                        // Extract solution.zip into student's folder under 'solution_extracted'
                        var extractTo = Path.Combine(dir, "solution_extracted");
                        try
                        {
                            if (Directory.Exists(extractTo))
                            {
                                _logger?.LogDebug("Cleaning previous extracted solution at {Path}", extractTo);
                                Directory.Delete(extractTo, true);
                            }

                            ZipFile.ExtractToDirectory(solutionZip, extractTo);
                            _logger?.LogInformation("Extracted inner solution {Zip} to {Path}", solutionZip, extractTo);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to extract solution.zip for {Folder}", folderName);
                            var violation = new Violation(folderName, ViolationType.MissingSolutionFile, $"Failed to extract solution.zip: {ex.Message}");
                            violations.Add(violation);
                            studentViolations.Add(violation.Message);
                            hasErrors = true;
                            
                            if (onStudentProgress != null)
                                await onStudentProgress(new StudentProgress(folderName, "failed", $"Extract failed: {ex.Message}", studentViolations));
                            return;
                        }

                        // Build the solution to verify it compiles and libraries are compatible
                        var solutionBuilder = new SolutionBuilder(_logger as ILogger<SolutionBuilder>);
                        var buildResult = await solutionBuilder.BuildSolutionAsync(extractTo, ct);
                        if (!buildResult.Success)
                        {
                            _logger?.LogWarning("Build failed for {Folder}: {Message}", folderName, buildResult.Output);
                            var violation = new Violation(folderName, ViolationType.BuildFailed, buildResult.Output);
                            violations.Add(violation);
                            studentViolations.Add($"Build failed: {buildResult.Output}");
                            hasErrors = true;
                            // Continue to normalize even if build fails
                        }
                        else
                        {
                            _logger?.LogInformation("Build succeeded for {Folder}", folderName);
                        }

                        // Collect C# files while ignoring trash
                        var csFiles = Directory.GetFiles(extractTo, "*.cs", SearchOption.AllDirectories)
                            .Where(f => !f.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
                                        && !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                                        && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                                        && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                            .ToArray();

                        _logger?.LogDebug("{Folder} -> found {Count} .cs files (after filtering)", folderName, csFiles.Length);

                        // If no cs files found, still create an empty normalized file
                        var normalizedText = NormalizeCsFiles(csFiles);

                        var outFile = Path.Combine(normalizedOutputDir, folderName + ".txt");
                        await System.IO.File.WriteAllTextAsync(outFile, normalizedText, ct);
                        _logger?.LogInformation("Wrote normalized file for {Folder} to {OutFile}", folderName, outFile);
                        
                        // Notify completion
                        if (onStudentProgress != null)
                            await onStudentProgress(new StudentProgress(folderName, hasErrors ? "completed_with_errors" : "completed", null, studentViolations));
                    }
                    catch (OperationCanceledException) 
                    { 
                        hasErrors = true;
                        if (onStudentProgress != null)
                            await onStudentProgress(new StudentProgress(folderName, "failed", "Cancelled", studentViolations));
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Unhandled error processing folder {Dir}", dir);
                        violations.Add(new Violation(folderName, ViolationType.BuildFailed, ex.Message));
                        hasErrors = true;
                        
                        if (onStudentProgress != null)
                            await onStudentProgress(new StudentProgress(folderName, "failed", ex.Message, studentViolations));
                    }
                    finally
                    {
                        if (hasErrors)
                            Interlocked.Increment(ref failed);
                        
                        Interlocked.Increment(ref completed);
                        onProgress?.Invoke(new ProcessingProgress(totalStudents, completed, failed));
                        sem.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);

            _logger?.LogInformation("Finished processing submissions. Violations found: {Count}", violations.Count);
            return violations.ToList();
        }

        private string NormalizeCsFiles(string[] csFiles)
        {
            var combined = string.Empty;
            foreach (var file in csFiles)
            {
                try
                {
                    var text = File.ReadAllText(file);
                    text = RemoveComments(text);
                    text = RemoveUsingDirectives(text);
                    combined += text + "\n";
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to read/normalize file {File}", file);
                    // ignore individual file read errors
                }
            }

            // Lowercase and collapse whitespace
            var lower = combined.ToLowerInvariant();
            var collapsed = Regex.Replace(lower, "\\s+", " ");
            return collapsed.Trim();
        }

        private string RemoveUsingDirectives(string text) => Regex.Replace(text, @"^\s*using\s+[^\r\n;]+;", "", RegexOptions.Multiline);

        private string RemoveComments(string text)
        {
            // Remove block comments
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            // Remove line comments
            text = Regex.Replace(text, @"//.*", string.Empty);
            return text;
        }
    }
}
