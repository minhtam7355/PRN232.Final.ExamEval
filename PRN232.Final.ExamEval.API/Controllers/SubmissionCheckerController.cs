using Microsoft.AspNetCore.Mvc;
using SubmitionsChecker;
using Microsoft.Extensions.Logging;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.IO.Compression;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    public class SubmissionCheckerController : ControllerBase
    {
        private readonly SubmissionProcessor _processor;
        private readonly ILogger<SubmissionCheckerController> _logger;

        public SubmissionCheckerController(SubmissionProcessor processor, ILogger<SubmissionCheckerController> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        /// <summary>
        /// Upload a single archive file (.zip or .rar) containing student submissions and run the full pipeline.
        /// Use "file" form field (multipart/form-data) in Swagger UI.
        /// </summary>
        [HttpPost("run")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Run([FromForm(Name = "file")] IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No archive uploaded. Provide a .zip or .rar file in the 'file' form field.");

            var tempRoot = Path.Combine(Path.GetTempPath(), "SubmissionPipeline");
            Directory.CreateDirectory(tempRoot);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var archiveFileName = Path.GetFileName(file.FileName);
            var savedPath = Path.Combine(tempRoot, timestamp + "_" + archiveFileName);

            await using (var fs = System.IO.File.Create(savedPath))
            {
                await file.CopyToAsync(fs, ct);
            }

            _logger.LogInformation("Saved uploaded archive to {Path}", savedPath);

            var extractedRootDir = Path.Combine(tempRoot, Path.GetFileNameWithoutExtension(savedPath));
            Directory.CreateDirectory(extractedRootDir);

            try
            {
                var ext = Path.GetExtension(savedPath).ToLowerInvariant();
                if (ext == ".zip")
                {
                    ZipFile.ExtractToDirectory(savedPath, extractedRootDir);
                }
                else if (ext == ".rar")
                {
                    using var stream = System.IO.File.OpenRead(savedPath);
                    using var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        var entry = reader.Entry;
                        if (!entry.IsDirectory)
                        {
                            var outPath = Path.Combine(extractedRootDir, entry.Key);
                            var dir = Path.GetDirectoryName(outPath);
                            if (!string.IsNullOrEmpty(dir))
                                Directory.CreateDirectory(dir);

                            using var entryStream = reader.OpenEntryStream();
                            await using var outFs = System.IO.File.Create(outPath);
                            await entryStream.CopyToAsync(outFs, ct);
                        }
                    }
                }
                else
                {
                    return BadRequest("Unsupported archive format. Use .zip or .rar");
                }

                _logger.LogInformation("Extracted archive to {Dir}", extractedRootDir);

                // normalized output location
                var normalizedOut = Path.Combine(tempRoot, "Normalized", timestamp);
                Directory.CreateDirectory(normalizedOut);

                var violations = await _processor.ProcessAsync(extractedRootDir, normalizedOut, ct);

                // Group violations by student folder and produce report entries only for students with at least one violation
                var violationsByStudent = violations
                    .GroupBy(v => v.StudentFolder)
                    .Select(g => new
                    {
                        StudentFolder = g.Key,
                        Issues = g.Select(v => new
                        {
                            Type = v.Type.ToString(),
                            Description = v.Message
                        }).ToArray()
                    })
                    .ToArray();

                var report = new
                {
                    Timestamp = timestamp,
                    SavedArchive = savedPath,
                    ExtractedRoot = extractedRootDir,
                    NormalizedOutput = normalizedOut,
                    Violations = violationsByStudent
                };

                return Ok(report);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Pipeline cancelled by request");
                return StatusCode(499, "Client Closed Request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline failed");
                return BadRequest(new { Message = "Pipeline failed", Detail = ex.Message });
            }
            finally
            {
                // keep saved files for debugging; remove if desired
                // try { System.IO.File.Delete(savedPath); } catch { }
            }
        }
    }
}
