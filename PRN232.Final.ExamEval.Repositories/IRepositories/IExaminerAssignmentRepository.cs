using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{
    public interface IExaminerAssignmentRepository
    {
        Task<IEnumerable<ExaminerAssignment>> GetAllAsync();
        Task<ExaminerAssignment?> GetByIdAsync(int id);
        Task<ExaminerAssignment> CreateAsync(ExaminerAssignment entity);
        Task<ExaminerAssignment?> UpdateAsync(int id, ExaminerAssignment entity);
        Task<bool> DeleteAsync(int id);
    }
}
