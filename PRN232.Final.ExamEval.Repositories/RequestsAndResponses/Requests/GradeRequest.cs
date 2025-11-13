using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests
{
    public class GradeRequest
    {
        public double Score { get; set; }
        public string? Comment { get; set; }
        public int SubmissionId { get; set; }
        public Guid ExaminerId { get; set; }
    }
}
