using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _repository;

        public SubjectService(ISubjectRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SubjectResponse>> GetAllAsync()
        {
            var subjects = await _repository.GetAllAsync();
            return subjects.Select(s => new SubjectResponse
            {
                SubjectId = s.SubjectId,
                Name = s.Name,
                Description = s.Description
            });
        }

        public async Task<SubjectResponse?> GetByIdAsync(int id)
        {
            var subject = await _repository.GetByIdAsync(id);
            return subject == null ? null : new SubjectResponse
            {
                SubjectId = subject.SubjectId,
                Name = subject.Name,
                Description = subject.Description
            };
        }

        public async Task<SubjectResponse> CreateAsync(SubjectRequest request)
        {
            var subject = new Subject
            {
                Name = request.Name,
                Description = request.Description
            };
            await _repository.CreateAsync(subject);
            return new SubjectResponse
            {
                SubjectId = subject.SubjectId,
                Name = subject.Name,
                Description = subject.Description
            };
        }

        public async Task<SubjectResponse?> UpdateAsync(int id, SubjectRequest request)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Name = request.Name;
            existing.Description = request.Description;
            await _repository.UpdateAsync(id, existing);

            return new SubjectResponse
            {
                SubjectId = existing.SubjectId,
                Name = existing.Name,
                Description = existing.Description
            };
        }

        public async Task<bool> DeleteAsync(int id) => await _repository.DeleteAsync(id);
    }
}
