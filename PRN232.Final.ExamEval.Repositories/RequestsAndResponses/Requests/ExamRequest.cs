using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests
{
    public class ExamRequest
    {
        public string Name { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int SubjectId { get; set; }
        public int SemesterId { get; set; }
    }
}

