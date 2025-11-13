using Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ILeopardAccountService
    {
        LeopardAccount? Authenticate(string email, string password);
        string? GetRoleName(LeopardAccount account);
        bool IsTokenAllowed(LeopardAccount account);
    }

}
