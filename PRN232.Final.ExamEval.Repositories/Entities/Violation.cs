using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class Violation
    {
        [Key]
        public int ViolationId { get; set; }

        [Required]
        public string Type { get; set; }

        public string? Description { get; set; }

        [Required]
        public bool IsConfirmed { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        // Navigation
        public virtual Submission Submission { get; set; }
    }
}
