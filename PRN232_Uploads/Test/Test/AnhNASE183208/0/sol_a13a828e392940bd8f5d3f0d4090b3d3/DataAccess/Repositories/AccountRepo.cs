using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class AccountRepo
    {
        private readonly SU25LeopardDBContext _context;

        public AccountRepo(SU25LeopardDBContext context)
        {
            _context = context;
        }
    }
    
}
