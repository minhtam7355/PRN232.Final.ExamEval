using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses
{
    public class SubmissionResponse
    {
        public int SubmissionId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public bool HasViolation { get; set; }
        public int ExamId { get; set; }
        public Guid StudentId { get; set; }
    }
}
