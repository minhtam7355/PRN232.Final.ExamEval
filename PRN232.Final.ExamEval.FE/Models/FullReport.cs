using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PRN232.Final.ExamEval.FE.Models
{
    public class FullReport
    {
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("savedArchive")]
        public string SavedArchive { get; set; }

        [JsonProperty("extractedRoot")]
        public string ExtractedRoot { get; set; }

        [JsonProperty("normalizedOutput")]
        public string NormalizedOutput { get; set; }

        [JsonProperty("summary")]
        public FullReportSummary Summary { get; set; }

        [JsonProperty("students")]
        public List<FullReportStudent> Students { get; set; }

        [JsonProperty("downloadLinks")]
        public FullReportDownloadLinks DownloadLinks { get; set; }
    }

    public class FullReportSummary
    {
        [JsonProperty("totalStudents")]
        public int TotalStudents { get; set; }

        [JsonProperty("passed")]
        public int Passed { get; set; }

        [JsonProperty("warning")]
        public int Warning { get; set; }

        [JsonProperty("failed")]
        public int Failed { get; set; }

        [JsonProperty("successRate")]
        public double SuccessRate { get; set; }

        [JsonProperty("plagiarismDetected")]
        public int PlagiarismDetected { get; set; }

        [JsonProperty("studentsWithPlagiarism")]
        public int StudentsWithPlagiarism { get; set; }
    }

    public class FullReportStudent
    {
        [JsonProperty("StudentId")]
        public string StudentId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("HasNormalizedFile")]
        public bool HasNormalizedFile { get; set; }

        [JsonProperty("NormalizedFilePath")]
        public string NormalizedFilePath { get; set; }

        [JsonProperty("IssueCount")]
        public int IssueCount { get; set; }

        [JsonProperty("Issues")]
        public List<IssueDetail> Issues { get; set; }

        [JsonProperty("PlagiarismDetected")]
        public bool PlagiarismDetected { get; set; }

        [JsonProperty("PlagiarismSimilarityMax")]
        public int? PlagiarismSimilarityMax { get; set; }

        [JsonProperty("SuspiciousGroupMembers")]
        public List<string> SuspiciousGroupMembers { get; set; }

        [JsonProperty("PlagiarismDetails")]
        public List<PlagiarismDetail> PlagiarismDetails { get; set; }

        // optional timestamps (sometimes not in summary but in progress)
        [JsonProperty("StartedAt")]
        public DateTime? StartedAt { get; set; }

        [JsonProperty("CompletedAt")]
        public DateTime? CompletedAt { get; set; }
    }

    public class FullReportDownloadLinks
    {
        [JsonProperty("plagiarismReportTxt")]
        public string PlagiarismReportTxt { get; set; }

        [JsonProperty("plagiarismResultsJson")]
        public string PlagiarismResultsJson { get; set; }

        [JsonProperty("fullReportJson")]
        public string FullReportJson { get; set; }

        [JsonProperty("allStudentsCombined")]
        public string AllStudentsCombined { get; set; }
    }
}
