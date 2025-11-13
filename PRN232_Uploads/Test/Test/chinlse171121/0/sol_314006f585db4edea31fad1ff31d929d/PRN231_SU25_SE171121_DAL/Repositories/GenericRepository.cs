using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_DAL.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<T?> GetAsync(Expression<Func<T, bool>> predicate) =>
            await _dbSet.FirstOrDefaultAsync(predicate);

        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.ToListAsync();

        public async Task<IEnumerable<T>> GetAllIncludingAsync(Expression<Func<T, object>> include)
        {
            return await _dbSet.Include(include).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllIncludingAsync(Expression<Func<T, object>> include, Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).Include(include).ToListAsync();
        }

        public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include)
        {
            return await _dbSet.Include(include).FirstOrDefaultAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}
