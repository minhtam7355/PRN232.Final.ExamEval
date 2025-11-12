using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime ExamDate { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        // Navigation
        public virtual Subject Subject { get; set; }
        public virtual Semester Semester { get; set; }
        public virtual ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<ExaminerAssignment> ExaminerAssignments { get; set; } = new List<ExaminerAssignment>();
    }
}
