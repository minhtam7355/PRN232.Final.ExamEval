using Repository.Entity;
using Repository.Interface;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{

    public class LeopardAccountService : ILeopardAccountService
    {
        private readonly ILeopardAccountRepository _repository;

        public LeopardAccountService(ILeopardAccountRepository repository)
        {
            _repository = repository;
        }

        public LeopardAccount? Authenticate(string email, string password)
        {
            return _repository.GetActiveAccountByEmailAndPassword(email, password);
        }

        public string? GetRoleName(LeopardAccount account)
        {
            return account.RoleId switch
            {
                5 => "administrator",
                6 => "moderator",
                7 => "developer",
                4 => "member",
                _ => null
            };
        }

        public bool IsTokenAllowed(LeopardAccount account)
        {
            return account.RoleId is 5 or 6 or 7 or 4;
        }
    }

}
