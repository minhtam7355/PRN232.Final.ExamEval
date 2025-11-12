using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class Grade
    {
        [Key]
        public int GradeId { get; set; }

        [Required]
        public double Score { get; set; }

        public string? Comment { get; set; }

        [Required]
        public DateTime GradedAt { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public Guid ExaminerId { get; set; }

        // Navigation
        public virtual Submission Submission { get; set; }
        public virtual User Examiner { get; set; }
    }
}
