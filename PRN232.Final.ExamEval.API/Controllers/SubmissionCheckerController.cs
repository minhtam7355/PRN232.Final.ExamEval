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
        private readonly ILoggerFactory _loggerFactory;

        public SubmissionCheckerController(
            SubmissionProcessor processor, 
            ILogger<SubmissionCheckerController> logger,
            IProgressTrackerService progressTracker,
            ILoggerFactory loggerFactory)
        {
            _processor = processor;
            _logger = logger;
            _progressTracker = progressTracker;
            _loggerFactory = loggerFactory;
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

            var folderId = Path.GetFileNameWithoutExtension(savedPath);
            var extractedRootDir = Path.Combine(baseRoot, folderId);
            Directory.CreateDirectory(extractedRootDir);

            // Check if job can start or needs to be queued
            var (canStart, queuePosition) = await _progressTracker.TryStartJob(folderId);

            if (!canStart)
            {
                // Job is queued
                _logger.LogInformation("Job {FolderId} queued at position {Position}", folderId, queuePosition);
                return Accepted(new
                {
                    folderId,
                    status = "queued",
                    queuePosition,
                    message = $"Upload accepted. Your job is queued at position {queuePosition}. Please wait for processing to start.",
                    signalRHub = "/hubs/progress"
                });
            }

            // Job can start immediately
            _logger.LogInformation("Job {FolderId} starting immediately", folderId);

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
                                var outPath = Path.Combine(extractedRootDir, entry.Key ?? "");
                                var dir = Path.GetDirectoryName(outPath);
                                if (!string.IsNullOrEmpty(dir))
                                    Directory.CreateDirectory(dir);

                                using var entryStream = reader.OpenEntryStream();
                                await using var outFs = System.IO.File.Create(outPath);
                                await entryStream.CopyToAsync(outFs);
                            }
                            else
                            {
                                var dirPath = Path.Combine(extractedRootDir, entry.Key ?? "");
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

                    // Run plagiarism detection WITH HISTORY (cross-submission comparison)
                    _logger.LogInformation("Starting CROSS-SUBMISSION plagiarism detection for {Count} students", allStudentDirs.Count);
                    
                    // Use global history storage path
                    var historyStoragePath = Path.Combine(AppContext.BaseDirectory, "PlagiarismHistory");
                    
                    // Create proper logger using factory
                    var plagDetectorLogger = _loggerFactory.CreateLogger<StyleBasedPlagiarismDetector>();
                    var plagiarismDetector = new StyleBasedPlagiarismDetector(plagDetectorLogger);
                    
                    _logger.LogInformation("📍 Root directory for plagiarism check: {Root}", Path.GetDirectoryName(normalizedOut));
                    
                    // Use the new method that compares with ALL previous submissions
                    var plagiarismResults = await plagiarismDetector.DetectPlagiarismWithHistoryAsync(
                        normalizedOut, 
                        historyStoragePath,
                        folderId, // Use folderId as submission ID
                        CancellationToken.None);
                    
                    // Generate plagiarism report
                    var plagiarismReportPath = Path.Combine(extractedRootDir, "plagiarism_report.txt");
                    await plagiarismDetector.GeneratePlagiarismReportAsync(plagiarismResults, plagiarismReportPath);
                    
                    // Create plagiarism lookup by student
                    var plagiarismByStudent = new Dictionary<string, List<PlagiarismResult>>();
                    foreach (var result in plagiarismResults)
                    {
                        if (!plagiarismByStudent.ContainsKey(result.Student1))
                            plagiarismByStudent[result.Student1] = new List<PlagiarismResult>();
                        if (!plagiarismByStudent.ContainsKey(result.Student2))
                            plagiarismByStudent[result.Student2] = new List<PlagiarismResult>();
                        
                        plagiarismByStudent[result.Student1].Add(result);
                        plagiarismByStudent[result.Student2].Add(result);
                    }

                    // Add plagiarism info to each student report
                    for (int i = 0; i < studentReports.Count; i++)
                    {
                        var studentReport = (dynamic)studentReports[i];
                        var studentName = (string)studentReport.StudentId;
                        
                        if (plagiarismByStudent.ContainsKey(studentName))
                        {
                            var plagiarismMatches = plagiarismByStudent[studentName];
                            var suspiciousGroups = plagiarismMatches
                                .Select((Func<PlagiarismResult, string>)(p => p.Student1 == studentName ? p.Student2 : p.Student1))
                                .Distinct()
                                .ToList();
                            
                            var maxSimilarity = plagiarismMatches.Max((Func<PlagiarismResult, double>)(p => p.SimilarityScore));
                            
                            studentReports[i] = new
                            {
                                studentReport.StudentId,
                                studentReport.Status,
                                studentReport.HasNormalizedFile,
                                studentReport.NormalizedFilePath,
                                studentReport.IssueCount,
                                studentReport.Issues,
                                PlagiarismDetected = true,
                                PlagiarismSimilarityMax = maxSimilarity,
                                SuspiciousGroupMembers = suspiciousGroups,
                                PlagiarismDetails = plagiarismMatches.Select((Func<PlagiarismResult, object>)(pm => new
                                {
                                    SimilarWithStudent = pm.Student1 == studentName ? pm.Student2 : pm.Student1,
                                    SimilarityScore = pm.SimilarityScore,
                                    Analysis = pm.Analysis
                                })).ToArray()
                            };
                        }
                        else
                        {
                            studentReports[i] = new
                            {
                                studentReport.StudentId,
                                studentReport.Status,
                                studentReport.HasNormalizedFile,
                                studentReport.NormalizedFilePath,
                                studentReport.IssueCount,
                                studentReport.Issues,
                                PlagiarismDetected = false,
                                PlagiarismSimilarityMax = (double?)null,
                                SuspiciousGroupMembers = new List<string>(),
                                PlagiarismDetails = Array.Empty<object>()
                            };
                        }
                    }

                    // Save plagiarism results as JSON
                    var plagiarismJsonPath = Path.Combine(extractedRootDir, "plagiarism_results.json");
                    var plagiarismJson = JsonSerializer.Serialize(new 
                    {
                        totalPairs = (allStudentDirs.Count * (allStudentDirs.Count - 1)) / 2,
                        suspiciousPairs = plagiarismResults.Count,
                        detectionMethod = "Style-Based (Variables & Namespaces)",
                        results = plagiarismResults.Select(r => new
                        {
                            student1 = r.Student1,
                            student2 = r.Student2,
                            similarityScore = r.SimilarityScore,
                            analysis = r.Analysis,
                            isSuspicious = r.IsSuspicious,
                            commonPatterns = r.CommonPatterns
                        }).ToArray()
                    }, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(plagiarismJsonPath, plagiarismJson);
                    
                    _logger.LogInformation("Plagiarism detection completed. Found {Count} suspicious pairs", plagiarismResults.Count);

                    var report = new
                    {
                        timestamp = timestamp,
                        savedArchive = savedPath,
                        extractedRoot = extractedRootDir,
                        normalizedOutput = normalizedOut,
                        summary = new
                        {
                            totalStudents = studentReports.Count,
                            passed = passedCount,
                            warning = warningCount,
                            failed = failedCount,
                            successRate = studentReports.Count > 0 
                                ? Math.Round((passedCount * 100.0) / studentReports.Count, 2) 
                                : 0,
                            plagiarismDetected = plagiarismResults.Count,
                            studentsWithPlagiarism = plagiarismByStudent.Count
                        },
                        students = studentReports,
                        downloadLinks = new
                        {
                            plagiarismReportTxt = $"/api/submissions/download/{folderId}/plagiarism_report.txt",
                            plagiarismResultsJson = $"/api/submissions/download/{folderId}/plagiarism_results.json",
                            fullReportJson = $"/api/submissions/report/{folderId}",
                            allStudentsCombined = $"/api/submissions/download/{folderId}/all_students_combined.txt"
                        }
                    };

                    var reportPath = Path.Combine(extractedRootDir, "report.json");
                    var reportJson = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(reportPath, reportJson);

                    await _progressTracker.CompleteJob(folderId, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background pipeline failed for {FolderId}", folderId);
                    await _progressTracker.CompleteJob(folderId, false, ex.Message);
                }
            });

            return Accepted(new 
            { 
                folderId, 
                status = "processing",
                queuePosition = 0,
                message = "Upload accepted. Processing started.",
                signalRHub = "/hubs/progress" 
            });
        }

        /// <summary>
        /// Create a combined file containing all students' code for plagiarism checking
        /// </summary>
        private async Task CreateCombinedCodeFile(string extractedRootDir, string normalizedOut, List<string> studentNames)
        {
            try
            {
                var combinedFilePath = Path.Combine(extractedRootDir, "all_students_combined.txt");
                var separatorLine = new string('=', 80);
                
                using var writer = new StreamWriter(combinedFilePath, false, System.Text.Encoding.UTF8);
                
                await writer.WriteLineAsync($"COMBINED CODE FILE FOR PLAGIARISM DETECTION");
                await writer.WriteLineAsync($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                await writer.WriteLineAsync($"Total Students: {studentNames.Count}");
                await writer.WriteLineAsync(separatorLine);
                await writer.WriteLineAsync();

                foreach (var studentName in studentNames.OrderBy(s => s))
                {
                    var normalizedFile = Path.Combine(normalizedOut, studentName + ".txt");
                    
                    if (System.IO.File.Exists(normalizedFile))
                    {
                        await writer.WriteLineAsync();
                        await writer.WriteLineAsync(separatorLine);
                        await writer.WriteLineAsync($"STUDENT: {studentName}");
                        await writer.WriteLineAsync(separatorLine);
                        await writer.WriteLineAsync();
                        
                        var content = await System.IO.File.ReadAllTextAsync(normalizedFile);
                        await writer.WriteLineAsync(content);
                        
                        await writer.WriteLineAsync();
                        await writer.WriteLineAsync($"END OF {studentName}");
                        await writer.WriteLineAsync();
                    }
                    else
                    {
                        await writer.WriteLineAsync();
                        await writer.WriteLineAsync(separatorLine);
                        await writer.WriteLineAsync($"STUDENT: {studentName}");
                        await writer.WriteLineAsync($"STATUS: NO CODE FILE (Missing or Failed)");
                        await writer.WriteLineAsync(separatorLine);
                        await writer.WriteLineAsync();
                    }
                }

                await writer.WriteLineAsync();
                await writer.WriteLineAsync(separatorLine);
                await writer.WriteLineAsync("END OF COMBINED FILE");
                await writer.WriteLineAsync(separatorLine);

                _logger.LogInformation("Created combined code file at {Path}", combinedFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create combined code file");
            }
        }

        /// <summary>
        /// Get queue status and all jobs
        /// </summary>
        [HttpGet("queue")]
        public async Task<IActionResult> GetQueue()
        {
            var allJobs = await _progressTracker.GetAllJobs();
            var queueLength = _progressTracker.GetQueueLength();

            return Ok(new
            {
                queueLength,
                jobs = allJobs.Select(j => new
                {
                    j.FolderId,
                    j.Status,
                    j.QueuePosition,
                    j.StartedAt,
                    j.Total,
                    j.Completed,
                    j.PercentComplete
                })
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

        /// <summary>
        /// Download generated files (plagiarism reports, combined code, etc.)
        /// </summary>
        [HttpGet("download/{folderId}/{fileName}")]
        public IActionResult DownloadFile(string folderId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(folderId) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest("folderId and fileName are required");

            // Security: only allow specific file names
            var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "plagiarism_report.txt",
                "plagiarism_results.json",
                "all_students_combined.txt",
                "report.json"
            };

            if (!allowedFiles.Contains(fileName))
                return BadRequest("Invalid file name");

            var baseRoot = Path.Combine(AppContext.BaseDirectory, "SubmissionPipeline");
            var extractedRootDir = Path.Combine(baseRoot, folderId);
            var filePath = Path.Combine(extractedRootDir, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "File not found", fileName });

            var contentType = fileName.EndsWith(".json") ? "application/json" : "text/plain";
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            return File(fileBytes, contentType, fileName);
        }

        /// <summary>
        /// Get plagiarism history summary - shows all submissions and students in history
        /// </summary>
        [HttpGet("plagiarism/history")]
        public async Task<IActionResult> GetPlagiarismHistory()
        {
            try
            {
                var historyStoragePath = Path.Combine(AppContext.BaseDirectory, "PlagiarismHistory");
                var historyManager = new PlagiarismHistoryManager(historyStoragePath, null);
                
                var summary = await historyManager.GetHistorySummaryAsync();
                var allStudents = await historyManager.GetAllStudentIdsAsync();

                return Ok(new
                {
                    totalSubmissions = summary.TotalSubmissions,
                    totalStudents = summary.TotalStudents,
                    lastUpdated = summary.LastUpdated,
                    allStudentIds = allStudents.OrderBy(s => s).ToList(),
                    submissions = summary.Submissions.OrderByDescending(s => s.Timestamp).Select(s => new
                    {
                        s.SubmissionId,
                        s.Timestamp,
                        s.StudentCount,
                        students = s.StudentIds.OrderBy(id => id).ToList()
                    }).ToList(),
                    message = $"History contains {summary.TotalSubmissions} submissions with {summary.TotalStudents} unique students. All future submissions will be compared against these."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get plagiarism history");
                return StatusCode(500, new { error = "Failed to retrieve plagiarism history", details = ex.Message });
            }
        }

        /// <summary>
        /// Clear all plagiarism history (USE WITH CAUTION!)
        /// This will delete all stored student codes and reset the detection system
        /// </summary>
        [HttpDelete("plagiarism/history")]
        public async Task<IActionResult> ClearPlagiarismHistory()
        {
            try
            {
                var historyStoragePath = Path.Combine(AppContext.BaseDirectory, "PlagiarismHistory");
                var historyManager = new PlagiarismHistoryManager(historyStoragePath, _logger as ILogger<PlagiarismHistoryManager>);
                
                var beforeSummary = await historyManager.GetHistorySummaryAsync();
                
                await historyManager.ClearHistoryAsync();
                
                _logger.LogWarning("⚠️ Plagiarism history cleared! Deleted {Submissions} submissions with {Students} students", 
                    beforeSummary.TotalSubmissions, beforeSummary.TotalStudents);

                return Ok(new
                {
                    message = "Plagiarism history cleared successfully",
                    deletedSubmissions = beforeSummary.TotalSubmissions,
                    deletedStudents = beforeSummary.TotalStudents,
                    warning = "All stored student codes have been deleted. Future plagiarism checks will start fresh."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear plagiarism history");
                return StatusCode(500, new { error = "Failed to clear plagiarism history", details = ex.Message });
            }
        }
    }
}
