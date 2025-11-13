using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PRN231_SU25_SE171121_DAL.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> GetAllAsync();

        Task<IEnumerable<T>> GetAllIncludingAsync(Expression<Func<T, object>> include);
        Task<IEnumerable<T>> GetAllIncludingAsync(Expression<Func<T, object>> include, Expression<Func<T, bool>> predicate);

        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include);

        Task AddAsync(T entity);
        void Remove(T entity);
    }
}
