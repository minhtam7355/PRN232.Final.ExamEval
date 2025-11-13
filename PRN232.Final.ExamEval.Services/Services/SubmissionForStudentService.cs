using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using PRN232.Final.ExamEval.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class SubmissionForStudentService : ISubmissionForStudentService
    {
        private readonly ISubmissionForStudentRepository _repository;

        public SubmissionForStudentService(ISubmissionForStudentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SubmissionResponse>> GetByStudentIdAsync(Guid studentId)
        {
            var submissions = await _repository.GetByStudentIdAsync(studentId);
            return submissions.Select(s => new SubmissionResponse
            {
                SubmissionId = s.SubmissionId,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                HasViolation = s.HasViolation,
                ExamId = s.ExamId,
                StudentId = s.StudentId
            });
        }

        public async Task<SubmissionResponse?> GetByIdAsync(int id)
        {
            var s = await _repository.GetByIdAsync(id);
            if (s == null) return null;

            return new SubmissionResponse
            {
                SubmissionId = s.SubmissionId,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                HasViolation = s.HasViolation,
                ExamId = s.ExamId,
                StudentId = s.StudentId
            };
        }

        public async Task<SubmissionResponse> CreateAsync(SubmissionRequest request, Guid studentId)
        {
            var entity = new Submission
            {
                FilePath = request.FilePath,
                SubmittedAt = DateTime.UtcNow,
                HasViolation = false,
                ExamId = request.ExamId,
                StudentId = studentId
            };

            await _repository.CreateAsync(entity);

            return new SubmissionResponse
            {
                SubmissionId = entity.SubmissionId,
                FilePath = entity.FilePath,
                SubmittedAt = entity.SubmittedAt,
                HasViolation = entity.HasViolation,
                ExamId = entity.ExamId,
                StudentId = entity.StudentId
            };
        }
    }
}
