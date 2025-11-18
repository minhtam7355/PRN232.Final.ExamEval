using System;
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
        BuildFailed
    }

    public record Violation(string StudentFolder, ViolationType Type, string Message);

    public class SubmissionProcessor
    {
        private readonly ILogger<SubmissionProcessor>? _logger;
        private static readonly Regex StudentFolderRegex = new Regex(@"^SE\d+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public SubmissionProcessor(ILogger<SubmissionProcessor>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process extracted submissions root directory. Each first-level folder is a student.
        /// Produces normalized text files under normalizedOutputDir and returns detected violations.
        /// </summary>
        public async Task<List<Violation>> ProcessAsync(string extractedRootDir, string normalizedOutputDir, CancellationToken ct = default)
        {
            _logger?.LogInformation("Start processing submissions from {Root} -> normalized into {Out}", extractedRootDir, normalizedOutputDir);

            var violations = new List<Violation>();

            if (!Directory.Exists(extractedRootDir))
            {
                _logger?.LogError("Extracted root directory not found: {Root}", extractedRootDir);
                throw new DirectoryNotFoundException($"Extracted root directory not found: {extractedRootDir}");
            }

            Directory.CreateDirectory(normalizedOutputDir);

            var studentDirs = Directory.GetDirectories(extractedRootDir);
            _logger?.LogInformation("Found {Count} student directories", studentDirs.Length);

            foreach (var dir in studentDirs)
            {
                ct.ThrowIfCancellationRequested();

                var folderName = Path.GetFileName(dir) ?? dir;
                _logger?.LogDebug("Processing student folder: {Folder}", folderName);

                if (!StudentFolderRegex.IsMatch(folderName))
                {
                    _logger?.LogWarning("Folder name does not match pattern: {Folder}", folderName);
                    violations.Add(new Violation(folderName, ViolationType.WrongFolderName, "Folder name does not match pattern SE\\d+"));
                }

                var zeroDir = Path.Combine(dir, "0");
                if (!Directory.Exists(zeroDir))
                {
                    _logger?.LogWarning("Missing folder '0' for {Folder}", folderName);
                    violations.Add(new Violation(folderName, ViolationType.MissingSolutionFile, "Missing folder '0'"));
                    continue;
                }

                var solutionZip = Path.Combine(zeroDir, "solution.zip");
                if (!File.Exists(solutionZip))
                {
                    _logger?.LogWarning("Missing solution.zip in 0/ for {Folder}", folderName);
                    violations.Add(new Violation(folderName, ViolationType.MissingSolutionFile, "Missing solution.zip in 0/"));
                    continue;
                }

                // Extract solution.zip
                var extractTo = Path.Combine(Path.GetTempPath(), "SubmissionExtract", folderName);
                try
                {
                    if (Directory.Exists(extractTo))
                        Directory.Delete(extractTo, true);

                    ZipFile.ExtractToDirectory(solutionZip, extractTo);
                    _logger?.LogInformation("Extracted {Zip} to {Path}", solutionZip, extractTo);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to extract solution.zip for {Folder}", folderName);
                    violations.Add(new Violation(folderName, ViolationType.MissingSolutionFile, $"Failed to extract solution.zip: {ex.Message}"));
                    continue;
                }

                // Build the solution to verify it compiles and libraries are compatible
                var solutionBuilder = new SolutionBuilder();
                var buildResult = await solutionBuilder.BuildSolutionAsync(extractTo, ct);
                if (!buildResult.Success)
                {
                    _logger?.LogWarning("Build failed for {Folder}: {Message}", folderName, buildResult.Output);
                    violations.Add(new Violation(folderName, ViolationType.BuildFailed, buildResult.Output));
                    // stop processing this student's submission
                    continue;
                }

                _logger?.LogInformation("Build succeeded for {Folder}", folderName);

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
                await File.WriteAllTextAsync(outFile, normalizedText, ct);
                _logger?.LogInformation("Wrote normalized file for {Folder} to {OutFile}", folderName, outFile);
            }

            _logger?.LogInformation("Finished processing submissions. Violations found: {Count}", violations.Count);
            return violations;
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
