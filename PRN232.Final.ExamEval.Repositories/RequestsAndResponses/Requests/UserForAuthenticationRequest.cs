using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests
{
    public class UserForAuthenticationRequest
    {
        [Required]
        public required string Email { get; init; }

        [Required]
        public required string Password { get; init; }
    }
}
