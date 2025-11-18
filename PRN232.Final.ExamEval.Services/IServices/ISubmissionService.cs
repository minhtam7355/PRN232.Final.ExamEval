using PRN232.Final.ExamEval.Repositories.Entities;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface ISubmissionService
    {
        Task<Submission?> GetSubmissionAsync(int submissionId, bool trackChanges = false);
        Task<IEnumerable<Submission>> GetSubmissionsByExamAsync(int examId, bool trackChanges = false);
        Task<IEnumerable<Submission>> GetSubmissionsByStudentAsync(Guid studentId, bool trackChanges = false);

        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> UpdateSubmissionAsync(Submission submission);
        Task<bool> DeleteSubmissionAsync(int submissionId);
    }
}
