using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public class SubmissionRepository : RepositoryBase<Submission>, ISubmissionRepository
    {
        public SubmissionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Submission?> GetSubmissionWithImagesAsync(int submissionId, bool trackChanges)
        {
            return await FindByCondition(s => s.SubmissionId == submissionId, trackChanges)
                .Include(s => s.Images)
                .Include(s => s.Exam)
                .Include(s => s.Student)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByExamAsync(int examId, bool trackChanges)
        {
            return await FindByCondition(s => s.ExamId == examId, trackChanges)
                .Include(s => s.Images)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByStudentAsync(Guid studentId, bool trackChanges)
        {
            return await FindByCondition(s => s.StudentId == studentId, trackChanges)
                .Include(s => s.Images)
                .ToListAsync();
        }
    }
}
