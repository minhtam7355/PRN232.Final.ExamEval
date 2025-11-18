using Microsoft.AspNetCore.Mvc;
using SubmitionsChecker;
using Microsoft.Extensions.Logging;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.IO.Compression;
using System.Text.Json;
using PRN232.Final.ExamEval.API.Services;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/submissions")]
    [ApiController]
    public class SubmissionCheckerController : ControllerBase
    {
        private readonly SubmissionProcessor _processor;
        private readonly ILogger<SubmissionCheckerController> _logger;
        private readonly IProgressTrackerService _progressTracker;

        public SubmissionCheckerController(
            SubmissionProcessor processor, 
            ILogger<SubmissionCheckerController> logger,
            IProgressTrackerService progressTracker)
        {
            _processor = processor;
            _logger = logger;
            _progressTracker = progressTracker;
        }

        /// <summary>
        /// Upload a single archive file (.zip or .rar) containing student submissions and start the pipeline in background.
        /// Returns immediately with the "folderId" which identifies the extracted folder where results will be placed.
        /// Use GET /api/submissions/report/{folderId} to fetch the JSON report when ready.
        /// Connect to SignalR hub at /hubs/progress and subscribe to folderId for real-time updates.
        /// </summary>
        [HttpPost("run")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Run([FromForm(Name = "file")] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No archive uploaded. Provide a .zip or .rar file in the 'file' form field.");

            var baseRoot = Path.Combine(AppContext.BaseDirectory, "SubmissionPipeline");
            Directory.CreateDirectory(baseRoot);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var archiveFileName = Path.GetFileName(file.FileName);
            var savedPath = Path.Combine(baseRoot, timestamp + "_" + archiveFileName);

            await using (var fs = System.IO.File.Create(savedPath))
            {
                await file.CopyToAsync(fs);
            }

            _logger.LogInformation("Saved uploaded archive to {Path}", savedPath);

            var folderId = Path.GetFileNameWithoutExtension(savedPath); // timestamp_filename
            var extractedRootDir = Path.Combine(baseRoot, folderId);
            Directory.CreateDirectory(extractedRootDir);

            // Start background processing
            _ = Task.Run(async () =>
            {
                try
                {
                    // extract archive
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
                                await entryStream.CopyToAsync(outFs);
                            }
                            else
                            {
                                var dirPath = Path.Combine(extractedRootDir, entry.Key);
                                Directory.CreateDirectory(dirPath);
                            }
                        }
                    }
                    else
                    {
                        await _progressTracker.CompleteJob(folderId, false, "Unsupported archive format");
                        return;
                    }

                    _logger.LogInformation("Extracted archive to {Dir}", extractedRootDir);

                    // normalized output location inside extracted folder
                    var normalizedOut = Path.Combine(extractedRootDir, "Normalized");
                    Directory.CreateDirectory(normalizedOut);

                    // Get student list for initialization
                    var allDirs = Directory.GetDirectories(extractedRootDir, "*", SearchOption.AllDirectories);
                    var studentDirs = allDirs
                        .Where(d =>
                        {
                            var name = Path.GetFileName(d);
                            if (string.Equals(name, "Normalized", StringComparison.OrdinalIgnoreCase)) return false;
                            if (name.StartsWith("_")) return false;
                            if (string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)) return false;
                            var zeroDir = Path.Combine(d, "0");
                            return Directory.Exists(zeroDir);
                        })
                        .Select(d => Path.GetFileName(d) ?? d)
                        .ToList();

                    // Initialize job tracking
                    await _progressTracker.InitializeJob(folderId, studentDirs);

                    // process students in parallel (degree 4) with detailed progress tracking
                    var violations = await _processor.ProcessAsync(
                        extractedRootDir, 
                        normalizedOut, 
                        maxDegreeOfParallelism: 4,
                        onProgress: (progress) =>
                        {
                            _progressTracker.UpdateProgress(
                                folderId, 
                                progress.Completed, 
                                progress.Failed, 
                                progress.CurrentStudent
                            ).Wait();
                        },
                        onStudentProgress: async (studentProgress) =>
                        {
                            await _progressTracker.UpdateStudentStatus(
                                folderId,
                                studentProgress.StudentName,
                                studentProgress.Status,
                                studentProgress.Error
                            );

                            // Add violations to tracking
                            if (studentProgress.Violations != null)
                            {
                                foreach (var violation in studentProgress.Violations)
                                {
                                    await _progressTracker.AddStudentViolation(
                                        folderId,
                                        studentProgress.StudentName,
                                        violation
                                    );
                                }
                            }
                        },
                        ct: CancellationToken.None);

                    // Get all processed students
                    var allStudentDirs = Directory.GetDirectories(extractedRootDir, "*", SearchOption.AllDirectories)
                        .Where(d =>
                        {
                            var name = Path.GetFileName(d);
                            if (string.Equals(name, "Normalized", StringComparison.OrdinalIgnoreCase)) return false;
                            if (name.StartsWith("_")) return false;
                            if (string.Equals(name, "0", StringComparison.OrdinalIgnoreCase)) return false;
                            var zeroDir = Path.Combine(d, "0");
                            return Directory.Exists(zeroDir);
                        })
                        .Select(d => Path.GetFileName(d) ?? d)
                        .ToList();

                    // Create violation lookup
                    var violationsByStudent = violations
                        .GroupBy(v => v.StudentFolder)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Build comprehensive student report
                    var studentReports = new List<object>();
                    foreach (var studentName in allStudentDirs)
                    {
                        var hasViolations = violationsByStudent.ContainsKey(studentName);
                        var studentViolations = hasViolations ? violationsByStudent[studentName] : new List<Violation>();
                        
                        // Check if normalized file exists
                        var normalizedFile = Path.Combine(normalizedOut, studentName + ".txt");
                        var hasNormalizedFile = System.IO.File.Exists(normalizedFile);
                        
                        // Determine status
                        string status;
                        if (!hasNormalizedFile)
                        {
                            status = "❌ FAILED - No output";
                        }
                        else if (studentViolations.Any(v => v.Type == ViolationType.BuildFailed))
                        {
                            status = "⚠️ WARNING - Build Failed";
                        }
                        else if (studentViolations.Any(v => v.Type == ViolationType.MissingSolutionFile))
                        {
                            status = "❌ FAILED - Missing Files";
                        }
                        else if (studentViolations.Any())
                        {
                            status = "⚠️ WARNING - Has Issues";
                        }
                        else
                        {
                            status = "✅ PASSED";
                        }

                        studentReports.Add(new
                        {
                            StudentId = studentName,
                            Status = status,
                            HasNormalizedFile = hasNormalizedFile,
                            NormalizedFilePath = hasNormalizedFile ? normalizedFile : null,
                            IssueCount = studentViolations.Count,
                            Issues = studentViolations.Select(v => new
                            {
                                Type = v.Type.ToString(),
                                Description = v.Message
                            }).ToArray()
                        });
                    }

                    // Sort by student ID
                    studentReports = studentReports.OrderBy(s => ((dynamic)s).StudentId).ToList();

                    // Calculate statistics
                    var passedCount = studentReports.Count(s => ((dynamic)s).Status.Contains("PASSED"));
                    var warningCount = studentReports.Count(s => ((dynamic)s).Status.Contains("WARNING"));
                    var failedCount = studentReports.Count(s => ((dynamic)s).Status.Contains("FAILED"));

                    var report = new
                    {
                        Timestamp = timestamp,
                        SavedArchive = savedPath,
                        ExtractedRoot = extractedRootDir,
                        NormalizedOutput = normalizedOut,
                        Summary = new
                        {
                            TotalStudents = studentReports.Count,
                            Passed = passedCount,
                            Warning = warningCount,
                            Failed = failedCount,
                            SuccessRate = studentReports.Count > 0 
                                ? Math.Round((passedCount * 100.0) / studentReports.Count, 2) 
                                : 0
                        },
                        Students = studentReports
                    };

                    var reportPath = Path.Combine(extractedRootDir, "report.json");
                    var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(reportPath, reportJson);

                    // Complete job
                    await _progressTracker.CompleteJob(folderId, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background pipeline failed for {FolderId}", folderId);
                    await _progressTracker.CompleteJob(folderId, false, ex.Message);
                }
            });

            // return immediate response with folder id
            return Accepted(new 
            { 
                folderId, 
                message = "Upload accepted. Processing started.",
                signalRHub = "/hubs/progress" 
            });
        }

        /// <summary>
        /// Get detailed progress for a job including per-student status.
        /// For real-time updates, connect to SignalR hub instead.
        /// </summary>
        [HttpGet("progress/{folderId}")]
        public async Task<IActionResult> GetProgress(string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return BadRequest("folderId is required");

            var progress = await _progressTracker.GetJobProgress(folderId);
            if (progress == null)
                return NotFound(new { error = "Job not found", folderId });

            return Ok(progress);
        }

        /// <summary>
        /// Get report for a previously uploaded folderId. If processing is ongoing, return 202 and current status.
        /// </summary>
        [HttpGet("report/{folderId}")]
        public async Task<IActionResult> GetReport(string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return BadRequest("folderId is required");

            var baseRoot = Path.Combine(AppContext.BaseDirectory, "SubmissionPipeline");
            var extractedRootDir = Path.Combine(baseRoot, folderId);
            if (!Directory.Exists(extractedRootDir))
                return NotFound(new { error = "folderId not found", folderId });

            var progress = await _progressTracker.GetJobProgress(folderId);
            if (progress != null && progress.Status == "processing")
            {
                return StatusCode(202, new 
                { 
                    folderId, 
                    status = "processing",
                    total = progress.Total,
                    completed = progress.Completed,
                    failed = progress.Failed,
                    percentComplete = progress.PercentComplete,
                    currentStudent = progress.CurrentStudent,
                    message = $"Processing {progress.Completed}/{progress.Total} students ({progress.PercentComplete}%)"
                });
            }

            var reportPath = Path.Combine(extractedRootDir, "report.json");
            if (!System.IO.File.Exists(reportPath))
                return NotFound(new { error = "Report not ready yet", folderId });

            var reportContent = await System.IO.File.ReadAllTextAsync(reportPath);
            return Content(reportContent, "application/json");
        }
    }
}
