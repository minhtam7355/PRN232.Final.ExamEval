using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests
{
    public class SubmissionRequest
    {
        public string FilePath { get; set; } = string.Empty;
        public int ExamId { get; set; }
    }
}
