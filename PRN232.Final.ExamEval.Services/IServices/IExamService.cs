using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IExamService
    {
        Task<IEnumerable<ExamResponse>> GetAllAsync();
        Task<ExamResponse?> GetByIdAsync(int id);
        Task<ExamResponse> CreateAsync(ExamRequest request);
        Task<ExamResponse?> UpdateAsync(int id, ExamRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
