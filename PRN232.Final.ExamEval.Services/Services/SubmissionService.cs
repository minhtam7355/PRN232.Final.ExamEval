using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly ISubmissionRepository _submissionRepo;
        private readonly IRepositoryManager _uow;

        public SubmissionService(
            ISubmissionRepository submissionRepo,
            IRepositoryManager uow)
        {
            _submissionRepo = submissionRepo;
            _uow = uow;
        }

        // ------------------------------------------------------
        // GET BY ID
        // ------------------------------------------------------
        public async Task<Submission?> GetSubmissionAsync(int submissionId, bool trackChanges = false)
        {
            return await _submissionRepo.GetSubmissionWithImagesAsync(submissionId, trackChanges);
        }

        // ------------------------------------------------------
        // GET BY EXAM
        // ------------------------------------------------------
        public async Task<IEnumerable<Submission>> GetSubmissionsByExamAsync(int examId, bool trackChanges = false)
        {
            return await _submissionRepo.GetSubmissionsByExamAsync(examId, trackChanges);
        }

        // ------------------------------------------------------
        // GET BY STUDENT
        // ------------------------------------------------------
        public async Task<IEnumerable<Submission>> GetSubmissionsByStudentAsync(Guid studentId, bool trackChanges = false)
        {
            return await _submissionRepo.GetSubmissionsByStudentAsync(studentId, trackChanges);
        }

        // ------------------------------------------------------
        // CREATE
        // ------------------------------------------------------
        public async Task<Submission> CreateSubmissionAsync(Submission submission)
        {
            _submissionRepo.Create(submission);
            await _uow.SaveAsync();    // Unit of Work commit
            return submission;
        }

        // ------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------
        public async Task<Submission?> UpdateSubmissionAsync(Submission submission)
        {
            var existing = await _submissionRepo
                .FindByCondition(x => x.SubmissionId == submission.SubmissionId, true)
                .FirstOrDefaultAsync();

            if (existing == null)
                return null;

            existing.FilePath = submission.FilePath;
            existing.SubmittedAt = submission.SubmittedAt;
            existing.HasViolation = submission.HasViolation;
            existing.ExamId = submission.ExamId;
            existing.StudentId = submission.StudentId;

            _submissionRepo.Update(existing);
            await _uow.SaveAsync();

            return existing;
        }

        // ------------------------------------------------------
        // DELETE
        // ------------------------------------------------------
        public async Task<bool> DeleteSubmissionAsync(int submissionId)
        {
            var existing = await _submissionRepo
                .FindByCondition(x => x.SubmissionId == submissionId, true)
                .FirstOrDefaultAsync();

            if (existing == null)
                return false;

            _submissionRepo.Delete(existing);
            await _uow.SaveAsync();

            return true;
        }
    }
}
