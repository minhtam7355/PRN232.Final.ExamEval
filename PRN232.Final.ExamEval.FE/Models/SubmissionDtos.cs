using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.FE.Models
{
    public class RunResponse
    {
        [JsonProperty("folderId")]
        public string FolderId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("queuePosition")]
        public int? QueuePosition { get; set; }
    }

    public class ProgressUpdate
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("completed")]
        public int Completed { get; set; }

        [JsonProperty("failed")]
        public int Failed { get; set; }

        [JsonProperty("percentComplete")]
        public int PercentComplete { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("currentStudent")]
        public string CurrentStudent { get; set; }

        [JsonProperty("students")]
        public Dictionary<string, StudentProgress> Students { get; set; }
    }

    public class StudentProgress
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("startedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("violations")]
        public List<string> Violations { get; set; }

        // NEW
        [JsonProperty("plagiarismDetected")]
        public bool PlagiarismDetected { get; set; }

        [JsonProperty("plagiarismSimilarityMax")]
        public int? PlagiarismSimilarityMax { get; set; }

        [JsonProperty("suspiciousGroupMembers")]
        public List<string> SuspiciousGroupMembers { get; set; }

        [JsonProperty("plagiarismDetails")]
        public List<PlagiarismDetail> PlagiarismDetails { get; set; }

        // Issue details for FAIL/WARNING
        [JsonProperty("issues")]
        public List<IssueDetail> Issues { get; set; }
    }

    public class PlagiarismDetail
    {
        [JsonProperty("similarWithStudent")]
        public string SimilarWithStudent { get; set; }

        [JsonProperty("similarityScore")]
        public int SimilarityScore { get; set; }

        [JsonProperty("analysis")]
        public string Analysis { get; set; }
    }

    public class IssueDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class ReportResponse
    {
        [JsonProperty("summary")]
        public ReportSummary Summary { get; set; }
    }

    public class ReportSummary
    {
        [JsonProperty("passed")]
        public int Passed { get; set; }

        [JsonProperty("warning")]
        public int Warning { get; set; }

        [JsonProperty("failed")]
        public int Failed { get; set; }

        [JsonProperty("successRate")]
        public double SuccessRate { get; set; }
    }

    public class StudentRow
    {
        public string StudentId { get; set; }
        public string Status { get; set; }
        public string Started { get; set; }
        public string Completed { get; set; }
        public string Violations { get; set; }

        public string PlagiarismInfo { get; set; }
        public string IssueDescription { get; set; }
    }
}
