using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLL.Repositories
{
    public class ProfileRepo(Su25leopardDbContext _context)
    {
        public async Task<List<LeopardProfile>> GetAll()
        {
            return await _context.LeopardProfiles.Include(x => x.LeopardType).ToListAsync();
        }

        public async Task<LeopardProfile> GetById(int id)
        {
            return await _context.LeopardProfiles.Include(x => x.LeopardType).FirstOrDefaultAsync(x => x.LeopardProfileId == id);
        }

        public async Task Add(LeopardProfile Profile)
        {
            await _context.LeopardProfiles.AddAsync(Profile);
            await _context.SaveChangesAsync();
        }

        public async Task Update(LeopardProfile Profile)
        {
            _context.LeopardProfiles.Update(Profile);
            await _context.SaveChangesAsync();
        }
        public async Task Delete(LeopardProfile handbag)
        {
            _context.LeopardProfiles.Remove(handbag);
            await _context.SaveChangesAsync();
        }
        public IQueryable<LeopardProfile> GetProfilesQueryable()
        {
            return _context.LeopardProfiles.Include(h => h.LeopardType);
        }
    }
}
