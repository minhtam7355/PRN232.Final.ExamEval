using PRN231_SU25_SE171121_DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_DAL.Repositories
{
    public interface IUnitOfWork
    {
        IGenericRepository<LeopardAccount> LeopardAccounts { get; }
        IGenericRepository<LeopardProfile> LeopardProfiles { get; }
        IGenericRepository<LeopardType> LeopardTypes { get; }

        Task<int> SaveChangesAsync();
    }
}
