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
    public class RubricService : IRubricService
    {
        private readonly IRubricRepository _repository;

        public RubricService(IRubricRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RubricResponse>> GetAllAsync()
        {
            var rubrics = await _repository.GetAllAsync();
            return rubrics.Select(r => new RubricResponse
            {
                RubricId = r.RubricId,
                Criteria = r.Criteria,
                MaxScore = r.MaxScore,
                ExamId = r.ExamId
            });
        }

        public async Task<RubricResponse?> GetByIdAsync(int id)
        {
            var rubric = await _repository.GetByIdAsync(id);
            return rubric == null ? null : new RubricResponse
            {
                RubricId = rubric.RubricId,
                Criteria = rubric.Criteria,
                MaxScore = rubric.MaxScore,
                ExamId = rubric.ExamId
            };
        }

        public async Task<RubricResponse> CreateAsync(RubricRequest request)
        {
            var rubric = new Rubric
            {
                Criteria = request.Criteria,
                MaxScore = request.MaxScore,
                ExamId = request.ExamId
            };

            await _repository.CreateAsync(rubric);

            return new RubricResponse
            {
                RubricId = rubric.RubricId,
                Criteria = rubric.Criteria,
                MaxScore = rubric.MaxScore,
                ExamId = rubric.ExamId
            };
        }

        public async Task<RubricResponse?> UpdateAsync(int id, RubricRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Criteria = request.Criteria;
            existing.MaxScore = request.MaxScore;
            existing.ExamId = request.ExamId;

            await _repository.UpdateAsync(id, existing);

            return new RubricResponse
            {
                RubricId = existing.RubricId,
                Criteria = existing.Criteria,
                MaxScore = existing.MaxScore,
                ExamId = existing.ExamId
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
