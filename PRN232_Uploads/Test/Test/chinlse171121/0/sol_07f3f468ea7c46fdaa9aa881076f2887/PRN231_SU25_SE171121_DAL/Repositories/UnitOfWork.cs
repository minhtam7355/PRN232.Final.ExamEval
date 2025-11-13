using PRN231_SU25_SE171121_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly Su25leopardDbContext _context;

        public IGenericRepository<LeopardAccount> LeopardAccounts { get; }
        public IGenericRepository<LeopardProfile> LeopardProfiles { get; }
        public IGenericRepository<LeopardType> LeopardTypes { get; }

        public UnitOfWork(Su25leopardDbContext context)
        {
            _context = context;
            LeopardAccounts = new GenericRepository<LeopardAccount>(_context);
            LeopardProfiles = new GenericRepository<LeopardProfile>(_context);
            LeopardTypes = new GenericRepository<LeopardType>(_context);
        }

        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
