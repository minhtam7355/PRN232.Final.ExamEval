using Microsoft.AspNetCore.SignalR;
using PRN232.Final.ExamEval.API.Hubs;
using System.Collections.Concurrent;
using System.Text.Json;

namespace PRN232.Final.ExamEval.API.Services
{
    public class StudentProcessingInfo
    {
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = "pending"; // pending, processing, completed, failed
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Error { get; set; }
        public List<string> Violations { get; set; } = new();
    }

    public class JobProgress
    {
        public string FolderId { get; set; } = string.Empty;
        public string Status { get; set; } = "processing"; // processing, done, failed
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
        public int Failed { get; set; }
        public int PercentComplete { get; set; }
        public string? CurrentStudent { get; set; }
        public ConcurrentDictionary<string, StudentProcessingInfo> Students { get; set; } = new();
    }

    public interface IProgressTrackerService
    {
        Task InitializeJob(string folderId, List<string> studentNames);
        Task UpdateStudentStatus(string folderId, string studentName, string status, string? error = null);
        Task AddStudentViolation(string folderId, string studentName, string violation);
        Task UpdateProgress(string folderId, int completed, int failed, string? currentStudent = null);
        Task CompleteJob(string folderId, bool success, string? error = null);
        Task<JobProgress?> GetJobProgress(string folderId);
    }

    public class ProgressTrackerService : IProgressTrackerService
    {
        private readonly IHubContext<SubmissionProgressHub> _hubContext;
        private readonly ConcurrentDictionary<string, JobProgress> _jobs = new();
        private readonly string _basePath;

        public ProgressTrackerService(IHubContext<SubmissionProgressHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _basePath = Path.Combine(AppContext.BaseDirectory, "SubmissionPipeline");
        }

        public async Task InitializeJob(string folderId, List<string> studentNames)
        {
            var job = new JobProgress
            {
                FolderId = folderId,
                Status = "processing",
                StartedAt = DateTime.UtcNow,
                Total = studentNames.Count,
                Completed = 0,
                Failed = 0,
                PercentComplete = 0
            };

            foreach (var name in studentNames)
            {
                job.Students[name] = new StudentProcessingInfo
                {
                    StudentName = name,
                    Status = "pending"
                };
            }

            _jobs[folderId] = job;
            await SaveToFile(folderId, job);
            await BroadcastProgress(folderId, job);
        }

        public async Task UpdateStudentStatus(string folderId, string studentName, string status, string? error = null)
        {
            if (_jobs.TryGetValue(folderId, out var job))
            {
                if (job.Students.TryGetValue(studentName, out var student))
                {
                    student.Status = status;
                    student.Error = error;

                    if (status == "processing")
                    {
                        student.StartedAt = DateTime.UtcNow;
                        job.CurrentStudent = studentName;
                    }
                    else if (status == "completed" || status == "failed")
                    {
                        student.CompletedAt = DateTime.UtcNow;
                    }

                    await SaveToFile(folderId, job);
                    await BroadcastProgress(folderId, job);
                }
            }
        }

        public async Task AddStudentViolation(string folderId, string studentName, string violation)
        {
            if (_jobs.TryGetValue(folderId, out var job))
            {
                if (job.Students.TryGetValue(studentName, out var student))
                {
                    student.Violations.Add(violation);
                    await SaveToFile(folderId, job);
                    await BroadcastProgress(folderId, job);
                }
            }
        }

        public async Task UpdateProgress(string folderId, int completed, int failed, string? currentStudent = null)
        {
            if (_jobs.TryGetValue(folderId, out var job))
            {
                job.Completed = completed;
                job.Failed = failed;
                job.PercentComplete = job.Total > 0 ? (int)((completed * 100.0) / job.Total) : 0;
                if (!string.IsNullOrEmpty(currentStudent))
                    job.CurrentStudent = currentStudent;

                await SaveToFile(folderId, job);
                await BroadcastProgress(folderId, job);
            }
        }

        public async Task CompleteJob(string folderId, bool success, string? error = null)
        {
            if (_jobs.TryGetValue(folderId, out var job))
            {
                job.Status = success ? "done" : "failed";
                job.FinishedAt = DateTime.UtcNow;
                job.PercentComplete = 100;
                job.CurrentStudent = null;

                if (!string.IsNullOrEmpty(error))
                {
                    // Could add error to job if needed
                }

                await SaveToFile(folderId, job);
                await BroadcastProgress(folderId, job);

                // Keep job in memory for 1 hour, then cleanup
                _ = Task.Delay(TimeSpan.FromHours(1)).ContinueWith(t => 
                {
                    _jobs.TryRemove(folderId, out JobProgress _);
                });
            }
        }

        public async Task<JobProgress?> GetJobProgress(string folderId)
        {
            if (_jobs.TryGetValue(folderId, out var job))
                return job;

            // Try loading from file if not in memory
            var statusPath = Path.Combine(_basePath, folderId, "progress.json");
            if (File.Exists(statusPath))
            {
                var json = await File.ReadAllTextAsync(statusPath);
                var loadedJob = JsonSerializer.Deserialize<JobProgress>(json);
                if (loadedJob != null)
                {
                    _jobs[folderId] = loadedJob;
                    return loadedJob;
                }
            }

            return null;
        }

        private async Task SaveToFile(string folderId, JobProgress job)
        {
            try
            {
                var dir = Path.Combine(_basePath, folderId);
                Directory.CreateDirectory(dir);
                
                var statusPath = Path.Combine(dir, "progress.json");
                var json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(statusPath, json);
            }
            catch
            {
                // Ignore file save errors, SignalR will still work
            }
        }

        private async Task BroadcastProgress(string folderId, JobProgress job)
        {
            try
            {
                // Broadcast to all clients subscribed to this job
                await _hubContext.Clients.Group(folderId).SendAsync("ProgressUpdate", job);
            }
            catch
            {
                // Ignore SignalR broadcast errors
            }
        }
    }
}
