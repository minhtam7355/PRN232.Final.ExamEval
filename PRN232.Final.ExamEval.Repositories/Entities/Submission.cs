using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class Submission
    {
        [Key]
        public int SubmissionId { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        [Required]
        public bool HasViolation { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        // Navigation
        public virtual Exam Exam { get; set; }
        public virtual User Student { get; set; }
        public virtual ICollection<Violation> Violations { get; set; } = new List<Violation>();
        public virtual ICollection<SubmissionImage> Images { get; set; } = new List<SubmissionImage>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    }
}
