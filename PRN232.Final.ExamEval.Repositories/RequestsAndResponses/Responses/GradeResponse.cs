using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses
{
    public class GradeResponse
    {
        public int GradeId { get; set; }
        public double Score { get; set; }
        public string? Comment { get; set; }
        public DateTime GradedAt { get; set; }
        public int SubmissionId { get; set; }
        public Guid ExaminerId { get; set; }
    }
}
