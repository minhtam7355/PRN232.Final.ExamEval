using Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interface
{
    public interface ILeopardAccountRepository
    {
        LeopardAccount? GetActiveAccountByEmailAndPassword(string email, string password);
    }

}
