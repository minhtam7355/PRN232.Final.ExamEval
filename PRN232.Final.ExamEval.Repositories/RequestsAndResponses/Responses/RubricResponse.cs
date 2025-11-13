using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses
{
    public class RubricResponse
    {
        public int RubricId { get; set; }
        public string Criteria { get; set; } = string.Empty;
        public double MaxScore { get; set; }
        public int ExamId { get; set; }
    }
}
