using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class SubmissionImageService : ISubmissionImageService
    {
        private readonly ISubmissionImageRepository _imageRepo;
        private readonly IRepositoryManager _uow;

        public SubmissionImageService(
            ISubmissionImageRepository imageRepo,
            IRepositoryManager uow)
        {
            _imageRepo = imageRepo;
            _uow = uow;
        }

        // ------------------------------------------------------
        // GET IMAGES BY SUBMISSION ID
        // ------------------------------------------------------
        public async Task<IEnumerable<SubmissionImage>> GetImagesAsync(int submissionId, bool trackChanges = false)
        {
            return await _imageRepo.GetImagesBySubmissionAsync(submissionId, trackChanges);
        }

        // ------------------------------------------------------
        // ADD IMAGE
        // ------------------------------------------------------
        public async Task<SubmissionImage> AddImageAsync(SubmissionImage image)
        {
            _imageRepo.Create(image);
            await _uow.SaveAsync();   // Unit of Work commit
            return image;
        }

        // ------------------------------------------------------
        // DELETE IMAGE
        // ------------------------------------------------------
        public async Task<bool> DeleteImageAsync(int submissionImageId)
        {
            var existing = await _imageRepo
                .FindByCondition(x => x.SubmissionImageId == submissionImageId, true)
                .FirstOrDefaultAsync();

            if (existing == null)
                return false;

            _imageRepo.Delete(existing);
            await _uow.SaveAsync();   // Unit of Work commit

            return true;
        }
    }
}
