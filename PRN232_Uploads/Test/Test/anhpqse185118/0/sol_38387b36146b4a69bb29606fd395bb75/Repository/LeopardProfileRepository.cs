using Microsoft.EntityFrameworkCore;
using Repository.Entity;
using Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class LeopardProfileRepository : ILeopardProfileRepository
    {
        private readonly LeopardDbContext _context;

        public LeopardProfileRepository(LeopardDbContext context)
        {
            _context = context;
        }

        public IEnumerable<LeopardProfile> GetAllWithType()
        {
            return _context.LeopardProfiles.Include(h => h.LeopardType).ToList();
        }

        public LeopardProfile GetById(int id)
        {
            return _context.LeopardProfiles.Include(h => h.LeopardType).FirstOrDefault(h => h.LeopardTypeId == id);
        }

        public void Add(LeopardProfile handbag)
        {
            _context.LeopardProfiles.Add(handbag);
            _context.SaveChanges();
        }

        public void Update(LeopardProfile handbag)
        {
            _context.LeopardProfiles.Update(handbag);
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var item = _context.LeopardProfiles.Find(id);
            if (item != null)
            {
                _context.LeopardProfiles.Remove(item);
                _context.SaveChanges();
            }
        }

        public IEnumerable<IGrouping<string, LeopardProfile>> Search(string name, string weight)
        {
            var query = _context.LeopardProfiles.Include(h => h.LeopardType).AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(h => h.LeopardName.Contains(name));
            if (!string.IsNullOrWhiteSpace(weight))
                query = query.Where(h => h.Weight.Equals(weight));

            return query.ToList().GroupBy(h => h.LeopardType.LeopardTypeName);
        }
    }

}
