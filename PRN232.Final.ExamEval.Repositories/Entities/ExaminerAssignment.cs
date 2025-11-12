using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class ExaminerAssignment
    {
        [Key]
        public int ExaminerAssignmentId { get; set; }

        [Required]
        public Guid ExaminerId { get; set; }

        [Required]
        public int ExamId { get; set; }

        // Navigation
        public virtual Exam Exam { get; set; }
        public virtual User Examiner { get; set; }
    }
}
