using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class UserProfile
    {
        [Key]
        public Guid UserId { get; set; }
        public string? Fullname { get; set; } = string.Empty;
        public string? Avatar { get; set; } = string.Empty;
        public string? Bio { get; set; } = string.Empty;
        public DateOnly? Birthday { get; set; } = null;
        public string? StudentID { get; set; } = string.Empty;
        public string? EmployeeID { get; set; } = string.Empty;

        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
