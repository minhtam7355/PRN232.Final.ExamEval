using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class Rubric
    {
        [Key]
        public int RubricId { get; set; }

        [Required]
        public string Criteria { get; set; }

        [Required]
        public double MaxScore { get; set; }

        [Required]
        public int ExamId { get; set; }

        // Navigation
        public virtual Exam Exam { get; set; }
    }
}
