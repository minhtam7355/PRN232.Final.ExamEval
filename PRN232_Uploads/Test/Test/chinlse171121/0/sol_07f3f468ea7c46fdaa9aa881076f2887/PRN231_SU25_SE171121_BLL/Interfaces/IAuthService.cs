using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_BLL.Interfaces
{
    public interface IAuthService
    {
        Task<(string token, string role)?> AuthenticateAsync(string email, string password);
    }
}
