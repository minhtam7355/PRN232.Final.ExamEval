using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class SemesterService : ISemesterService
    {
        private readonly ISemesterRepository _repository;

        public SemesterService(ISemesterRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SemesterResponse>> GetAllAsync()
        {
            var semesters = await _repository.GetAllAsync();
            return semesters.Select(s => new SemesterResponse
            {
                SemesterId = s.SemesterId,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            });
        }

        public async Task<SemesterResponse?> GetByIdAsync(int id)
        {
            var semester = await _repository.GetByIdAsync(id);
            return semester == null ? null : new SemesterResponse
            {
                SemesterId = semester.SemesterId,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate
            };
        }

        public async Task<SemesterResponse> CreateAsync(SemesterRequest request)
        {
            var entity = new Semester
            {
                Name = request.Name,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            await _repository.CreateAsync(entity);

            return new SemesterResponse
            {
                SemesterId = entity.SemesterId,
                Name = entity.Name,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            };
        }

        public async Task<SemesterResponse?> UpdateAsync(int id, SemesterRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Name = request.Name;
            existing.StartDate = request.StartDate;
            existing.EndDate = request.EndDate;

            await _repository.UpdateAsync(id, existing);

            return new SemesterResponse
            {
                SemesterId = existing.SemesterId,
                Name = existing.Name,
                StartDate = existing.StartDate,
                EndDate = existing.EndDate
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
