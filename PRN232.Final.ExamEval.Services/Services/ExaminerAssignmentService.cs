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
    public class ExaminerAssignmentService : IExaminerAssignmentService
    {
        private readonly IExaminerAssignmentRepository _repository;

        public ExaminerAssignmentService(IExaminerAssignmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ExaminerAssignmentResponse>> GetAllAsync()
        {
            var data = await _repository.GetAllAsync();
            return data.Select(e => new ExaminerAssignmentResponse
            {
                ExaminerAssignmentId = e.ExaminerAssignmentId,
                ExaminerId = e.ExaminerId,
                ExamId = e.ExamId
            });
        }

        public async Task<ExaminerAssignmentResponse?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : new ExaminerAssignmentResponse
            {
                ExaminerAssignmentId = entity.ExaminerAssignmentId,
                ExaminerId = entity.ExaminerId,
                ExamId = entity.ExamId
            };
        }

        public async Task<ExaminerAssignmentResponse> CreateAsync(ExaminerAssignmentRequest request)
        {
            var entity = new ExaminerAssignment
            {
                ExaminerId = request.ExaminerId,
                ExamId = request.ExamId
            };

            await _repository.CreateAsync(entity);

            return new ExaminerAssignmentResponse
            {
                ExaminerAssignmentId = entity.ExaminerAssignmentId,
                ExaminerId = entity.ExaminerId,
                ExamId = entity.ExamId
            };
        }

        public async Task<ExaminerAssignmentResponse?> UpdateAsync(int id, ExaminerAssignmentRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.ExaminerId = request.ExaminerId;
            existing.ExamId = request.ExamId;

            await _repository.UpdateAsync(id, existing);

            return new ExaminerAssignmentResponse
            {
                ExaminerAssignmentId = existing.ExaminerAssignmentId,
                ExaminerId = existing.ExaminerId,
                ExamId = existing.ExamId
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
