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
    public class GradeService : IGradeService
    {
        private readonly IGradeRepository _repository;

        public GradeService(IGradeRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GradeResponse>> GetAllAsync()
        {
            var grades = await _repository.GetAllAsync();
            return grades.Select(g => new GradeResponse
            {
                GradeId = g.GradeId,
                Score = g.Score,
                Comment = g.Comment,
                GradedAt = g.GradedAt,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.ExaminerId
            });
        }

        public async Task<GradeResponse?> GetByIdAsync(int id)
        {
            var g = await _repository.GetByIdAsync(id);
            if (g == null) return null;

            return new GradeResponse
            {
                GradeId = g.GradeId,
                Score = g.Score,
                Comment = g.Comment,
                GradedAt = g.GradedAt,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.ExaminerId
            };
        }

        public async Task<GradeResponse> CreateAsync(GradeRequest request)
        {
            var entity = new Grade
            {
                Score = request.Score,
                Comment = request.Comment,
                GradedAt = DateTime.UtcNow,
                SubmissionId = request.SubmissionId,
                ExaminerId = request.ExaminerId
            };

            await _repository.CreateAsync(entity);

            return new GradeResponse
            {
                GradeId = entity.GradeId,
                Score = entity.Score,
                Comment = entity.Comment,
                GradedAt = entity.GradedAt,
                SubmissionId = entity.SubmissionId,
                ExaminerId = entity.ExaminerId
            };
        }

        public async Task<GradeResponse?> UpdateAsync(int id, GradeRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Score = request.Score;
            existing.Comment = request.Comment;
            existing.GradedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(id, existing);

            return new GradeResponse
            {
                GradeId = existing.GradeId,
                Score = existing.Score,
                Comment = existing.Comment,
                GradedAt = existing.GradedAt,
                SubmissionId = existing.SubmissionId,
                ExaminerId = existing.ExaminerId
            };
        }

        public async Task<bool> DeleteAsync(int id) =>
            await _repository.DeleteAsync(id);
    }
}
