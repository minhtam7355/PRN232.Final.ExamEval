using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{
    public interface ISubmissionForStudentRepository
    {
        Task<IEnumerable<Submission>> GetByStudentIdAsync(Guid studentId);
        Task<Submission?> GetByIdAsync(int id);
        Task<Submission> CreateAsync(Submission submission);
    }
}
