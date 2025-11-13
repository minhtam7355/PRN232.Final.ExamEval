using DLL;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLL.Repositories
{
    public class AccountRepo
    {
        protected readonly Su25leopardDbContext _context;
        public AccountRepo(Su25leopardDbContext context)
        {
            _context = context;
        }
        public async Task<LeopardAccount?> Login(string email, string password)
        {
            var response = await _context.LeopardAccounts.FirstOrDefaultAsync(x => x.Email == email && x.Password == password);
            if (response.RoleId == 4 || response.RoleId == 5 || response.RoleId == 6 || response.RoleId == 7)
            {
                return response;
            }
            return null;
        }
    }
}
