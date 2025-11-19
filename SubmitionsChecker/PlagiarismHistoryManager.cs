using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SubmitionsChecker
{
    /// <summary>
    /// Manages plagiarism history across all submissions
    /// Stores all student codes for cross-submission comparison
    /// </summary>
    public class PlagiarismHistoryManager
    {
        private readonly string _historyStoragePath;
        private readonly ILogger<PlagiarismHistoryManager>? _logger;
        private const string HistoryFileName = "plagiarism_history.json";
        private const string CodesDirectoryName = "student_codes";

        public PlagiarismHistoryManager(string storagePath, ILogger<PlagiarismHistoryManager>? logger = null)
        {
            _historyStoragePath = storagePath;
            _logger = logger;
            Directory.CreateDirectory(_historyStoragePath);
            Directory.CreateDirectory(Path.Combine(_historyStoragePath, CodesDirectoryName));
        }

        /// <summary>
        /// Save current submission codes to history
        /// </summary>
        public async Task SaveSubmissionAsync(string submissionId, Dictionary<string, string> studentCodes)
        {
            var timestamp = DateTime.UtcNow;
            var codesDir = Path.Combine(_historyStoragePath, CodesDirectoryName);

            foreach (var (studentId, code) in studentCodes)
            {
                var studentDir = Path.Combine(codesDir, studentId);
                Directory.CreateDirectory(studentDir);

                // Save code with submission info
                var codeFile = Path.Combine(studentDir, $"{submissionId}_{timestamp:yyyyMMddHHmmss}.cs");
                await File.WriteAllTextAsync(codeFile, code);

                _logger?.LogDebug("Saved code for {Student} from submission {SubmissionId}", studentId, submissionId);
            }

            // Update history index
            await UpdateHistoryIndexAsync(submissionId, studentCodes.Keys.ToList(), timestamp);
        }

        /// <summary>
        /// Get all student codes from history (including current submission)
        /// </summary>
        public async Task<Dictionary<string, string>> GetAllStudentCodesAsync()
        {
            var allCodes = new Dictionary<string, string>();
            var codesDir = Path.Combine(_historyStoragePath, CodesDirectoryName);

            if (!Directory.Exists(codesDir))
                return allCodes;

            // Get all student directories
            var studentDirs = Directory.GetDirectories(codesDir);

            foreach (var studentDir in studentDirs)
            {
                var studentId = Path.GetFileName(studentDir);
                
                // Get all code files for this student (may have multiple submissions)
                var codeFiles = Directory.GetFiles(studentDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                    .ToArray();

                if (codeFiles.Length > 0)
                {
                    // Use the most recent submission
                    var latestCode = await File.ReadAllTextAsync(codeFiles[0]);
                    allCodes[studentId] = latestCode;

                    _logger?.LogDebug("Loaded code for {Student} from {File}", studentId, Path.GetFileName(codeFiles[0]));
                }
            }

            return allCodes;
        }

        /// <summary>
        /// Get history summary
        /// </summary>
        public async Task<PlagiarismHistorySummary> GetHistorySummaryAsync()
        {
            var historyFile = Path.Combine(_historyStoragePath, HistoryFileName);
            
            if (!File.Exists(historyFile))
            {
                return new PlagiarismHistorySummary
                {
                    TotalSubmissions = 0,
                    TotalStudents = 0,
                    Submissions = new List<SubmissionRecord>()
                };
            }

            var json = await File.ReadAllTextAsync(historyFile);
            var summary = JsonSerializer.Deserialize<PlagiarismHistorySummary>(json);
            
            return summary ?? new PlagiarismHistorySummary
            {
                TotalSubmissions = 0,
                TotalStudents = 0,
                Submissions = new List<SubmissionRecord>()
            };
        }

        /// <summary>
        /// Clear all history (use with caution)
        /// </summary>
        public async Task ClearHistoryAsync()
        {
            var codesDir = Path.Combine(_historyStoragePath, CodesDirectoryName);
            if (Directory.Exists(codesDir))
            {
                Directory.Delete(codesDir, true);
                Directory.CreateDirectory(codesDir);
            }

            var historyFile = Path.Combine(_historyStoragePath, HistoryFileName);
            if (File.Exists(historyFile))
            {
                File.Delete(historyFile);
            }

            _logger?.LogInformation("Cleared all plagiarism history");
        }

        /// <summary>
        /// Get all students across all submissions
        /// </summary>
        public async Task<List<string>> GetAllStudentIdsAsync()
        {
            var codesDir = Path.Combine(_historyStoragePath, CodesDirectoryName);
            
            if (!Directory.Exists(codesDir))
                return new List<string>();

            var studentDirs = Directory.GetDirectories(codesDir);
            return studentDirs.Select(d => Path.GetFileName(d)).ToList();
        }

        private async Task UpdateHistoryIndexAsync(string submissionId, List<string> studentIds, DateTime timestamp)
        {
            var historyFile = Path.Combine(_historyStoragePath, HistoryFileName);
            
            PlagiarismHistorySummary summary;
            
            if (File.Exists(historyFile))
            {
                var json = await File.ReadAllTextAsync(historyFile);
                summary = JsonSerializer.Deserialize<PlagiarismHistorySummary>(json) ?? new PlagiarismHistorySummary();
            }
            else
            {
                summary = new PlagiarismHistorySummary
                {
                    Submissions = new List<SubmissionRecord>()
                };
            }

            // Add new submission record
            summary.Submissions.Add(new SubmissionRecord
            {
                SubmissionId = submissionId,
                Timestamp = timestamp,
                StudentCount = studentIds.Count,
                StudentIds = studentIds
            });

            // Update totals
            summary.TotalSubmissions = summary.Submissions.Count;
            var allStudents = new HashSet<string>();
            foreach (var submission in summary.Submissions)
            {
                foreach (var studentId in submission.StudentIds)
                {
                    allStudents.Add(studentId);
                }
            }
            summary.TotalStudents = allStudents.Count;
            summary.LastUpdated = timestamp;

            // Save updated index
            var updatedJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(historyFile, updatedJson);

            _logger?.LogInformation("Updated plagiarism history index. Total submissions: {Total}, Total students: {Students}", 
                summary.TotalSubmissions, summary.TotalStudents);
        }
    }

    public class PlagiarismHistorySummary
    {
        public int TotalSubmissions { get; set; }
        public int TotalStudents { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<SubmissionRecord> Submissions { get; set; } = new();
    }

    public class SubmissionRecord
    {
        public string SubmissionId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int StudentCount { get; set; }
        public List<string> StudentIds { get; set; } = new();
    }
}

