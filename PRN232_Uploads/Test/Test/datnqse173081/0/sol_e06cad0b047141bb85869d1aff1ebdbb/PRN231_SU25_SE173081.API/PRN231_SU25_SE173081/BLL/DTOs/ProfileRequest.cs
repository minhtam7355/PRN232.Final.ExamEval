using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs
{
    public class ProfileRequest
    {
        public int LeopardProfileId { get; set; }
        [Required]
        public int LeopardTypeId { get; set; }
        [Required]
        [RegularExpression(@"^([A-Z0-9][a-zA-Z0-9#]*\s)*([A-Z0-9][a-zA-Z0-9#]*)$",
        ErrorMessage = "name format is invalid")]
        public string LeopardName { get; set; } = null!;
        [Required]

        public double Weight { get; set; }
        [Required]

        public string Characteristics { get; set; } = null!;
        [Required]

        public string CareNeeds { get; set; } = null!;
        [Required]

        public DateTime ModifiedDate { get; set; }
    }
}
