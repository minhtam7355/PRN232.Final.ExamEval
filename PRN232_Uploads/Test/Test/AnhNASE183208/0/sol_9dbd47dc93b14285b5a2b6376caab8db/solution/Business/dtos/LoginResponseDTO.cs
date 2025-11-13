using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.dtos
{
    public class LoginResponseDTO
    {
        public String Token { get; set; }
        public int Role { get; set; }
    }
}
