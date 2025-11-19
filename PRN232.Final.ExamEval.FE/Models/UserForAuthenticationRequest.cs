using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.FE.Models
{
    internal class UserForAuthenticationRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
