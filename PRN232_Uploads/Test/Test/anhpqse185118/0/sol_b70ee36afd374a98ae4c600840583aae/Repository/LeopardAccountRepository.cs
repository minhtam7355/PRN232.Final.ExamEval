using Repository.Entity;
using Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class LeopardAccountRepository : ILeopardAccountRepository
    {
        private readonly LeopardDbContext _context;

        public LeopardAccountRepository(LeopardDbContext context)
        {
            _context = context;
        }

        public LeopardAccount? GetActiveAccountByEmailAndPassword(string email, string password)
        {
            return _context.LeopardAccounts
                .FirstOrDefault(a => a.Email == email && a.Password == password);
        }
    }

}
