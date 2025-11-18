using PRN232.Final.ExamEval.Repositories.Entities;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface ISubmissionImageService
    {
        Task<IEnumerable<SubmissionImage>> GetImagesAsync(int submissionId, bool trackChanges = false);
        Task<SubmissionImage> AddImageAsync(SubmissionImage image);
        Task<bool> DeleteImageAsync(int submissionImageId);
    }
}
