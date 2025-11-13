using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class LeopardProfileRepo
    {
        private SU25LeopardDBContext _context;

        public async Task<LeopardAccount> GetUserAccount(string email, string password)
        {
            _context = new();
            return _context.LeopardAccounts.FirstOrDefault(s => s.Email == email && s.Password == password);
        }
    }
}
