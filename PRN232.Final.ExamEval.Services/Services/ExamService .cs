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
    public class ExamService : IExamService
    {
        private readonly IExamRepository _repository;

        public ExamService(IExamRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<ExamResponse>> GetAllAsync()
        {
            var exams = await _repository.GetAllAsync();
            return exams.Select(e => new ExamResponse
            {
                ExamId = e.ExamId,
                Name = e.Name,
                ExamDate = e.ExamDate,
                SubjectId = e.SubjectId,
                SemesterId = e.SemesterId
            });
        }

        public async Task<ExamResponse?> GetByIdAsync(int id)
        {
            var exam = await _repository.GetByIdAsync(id);
            return exam == null ? null : new ExamResponse
            {
                ExamId = exam.ExamId,
                Name = exam.Name,
                ExamDate = exam.ExamDate,
                SubjectId = exam.SubjectId,
                SemesterId = exam.SemesterId
            };
        }

        public async Task<ExamResponse> CreateAsync(ExamRequest request)
        {
            var exam = new Exam
            {
                Name = request.Name,
                ExamDate = request.ExamDate,
                SubjectId = request.SubjectId,
                SemesterId = request.SemesterId
            };

            await _repository.CreateAsync(exam);

            return new ExamResponse
            {
                ExamId = exam.ExamId,
                Name = exam.Name,
                ExamDate = exam.ExamDate,
                SubjectId = exam.SubjectId,
                SemesterId = exam.SemesterId
            };
        }

        public async Task<ExamResponse?> UpdateAsync(int id, ExamRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Name = request.Name;
            existing.ExamDate = request.ExamDate;
            existing.SubjectId = request.SubjectId;
            existing.SemesterId = request.SemesterId;

            await _repository.UpdateAsync(id, existing);

            return new ExamResponse
            {
                ExamId = existing.ExamId,
                Name = existing.Name,
                ExamDate = existing.ExamDate,
                SubjectId = existing.SubjectId,
                SemesterId = existing.SemesterId
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
