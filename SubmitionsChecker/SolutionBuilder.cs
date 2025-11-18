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

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{slnPath}\" -c Release",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(slnPath) ?? extractedRoot
            };

            var outputBuilder = new StringBuilder();

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

            try
            {
                _logger?.LogInformation("Starting dotnet build for {Sln}", slnPath);
                if (!process.Start())
                {
                    return new BuildResult(false, "Failed to start dotnet build process.");
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
                var output = outputBuilder.ToString();
                _logger?.LogInformation("dotnet build exited with code {Code}", exitCode);

                var success = exitCode == 0;
                return new BuildResult(success, output);
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(true); } catch { }
                return new BuildResult(false, "Build cancelled.");
            }
            catch (Exception ex)
            {
                return new BuildResult(false, ex.Message + "\n" + outputBuilder.ToString());
            }
        }
    }
}
