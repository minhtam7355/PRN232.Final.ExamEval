using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IRubricService
    {
        Task<IEnumerable<RubricResponse>> GetAllAsync();
        Task<RubricResponse?> GetByIdAsync(int id);
        Task<RubricResponse> CreateAsync(RubricRequest request);
        Task<RubricResponse?> UpdateAsync(int id, RubricRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
