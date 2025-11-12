using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses
{
    public class UserForReturnResponse
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        // From UserProfile
        public string? Fullname { get; set; }
        public string? Avatar { get; set; }
        public string? Bio { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? StudentID { get; set; }
        public string? EmployeeID { get; set; }

        // Roles
        public List<string> Roles { get; set; } = new();
    }
}
