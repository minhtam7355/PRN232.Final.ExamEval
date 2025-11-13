using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponse>> GetAllAsync();
        Task<SubjectResponse?> GetByIdAsync(int id);
        Task<SubjectResponse> CreateAsync(SubjectRequest request);
        Task<SubjectResponse?> UpdateAsync(int id, SubjectRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
