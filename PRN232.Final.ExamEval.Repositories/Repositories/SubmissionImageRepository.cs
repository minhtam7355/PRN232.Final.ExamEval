using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public class SubmissionImageRepository : RepositoryBase<SubmissionImage>, ISubmissionImageRepository
    {
        public SubmissionImageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SubmissionImage>> GetImagesBySubmissionAsync(int submissionId, bool trackChanges)
        {
            return await FindByCondition(img => img.SubmissionId == submissionId, trackChanges)
                .ToListAsync();
        }
    }
}
