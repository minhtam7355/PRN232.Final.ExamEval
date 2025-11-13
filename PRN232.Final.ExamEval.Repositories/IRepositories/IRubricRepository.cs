using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{
    public interface IRubricRepository
    {
        Task<IEnumerable<Rubric>> GetAllAsync();
        Task<Rubric?> GetByIdAsync(int id);
        Task<Rubric> CreateAsync(Rubric rubric);
        Task<Rubric?> UpdateAsync(int id, Rubric rubric);
        Task<bool> DeleteAsync(int id);
    }
}

