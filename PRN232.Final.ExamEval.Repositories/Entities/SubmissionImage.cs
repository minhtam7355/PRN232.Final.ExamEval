using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Entities
{
    public class SubmissionImage
    {
        [Key]
        public int SubmissionImageId { get; set; }

        [Required]
        public string ImagePath { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        // Navigation
        public virtual Submission Submission { get; set; }
    }
}
