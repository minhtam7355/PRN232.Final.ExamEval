using PRN232.Final.ExamEval.Repositories.Entities;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{
    public interface ISubmissionImageRepository : IRepositoryBase<SubmissionImage>
    {
        Task<IEnumerable<SubmissionImage>> GetImagesBySubmissionAsync(int submissionId, bool trackChanges);
    }
}
