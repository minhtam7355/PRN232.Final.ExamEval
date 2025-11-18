using PRN232.Final.ExamEval.Repositories.Entities;

namespace PRN232.Final.ExamEval.Repositories.IRepositories
{
    public interface ISubmissionRepository : IRepositoryBase<Submission>
    {
        Task<Submission?> GetSubmissionWithImagesAsync(int submissionId, bool trackChanges);
        Task<IEnumerable<Submission>> GetSubmissionsByExamAsync(int examId, bool trackChanges);
        Task<IEnumerable<Submission>> GetSubmissionsByStudentAsync(Guid studentId, bool trackChanges);
    }
}
