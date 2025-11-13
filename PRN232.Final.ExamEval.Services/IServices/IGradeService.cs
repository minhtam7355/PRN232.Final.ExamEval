using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IGradeService
    {
        Task<IEnumerable<GradeResponse>> GetAllAsync();
        Task<GradeResponse?> GetByIdAsync(int id);
        Task<GradeResponse> CreateAsync(GradeRequest request);
        Task<GradeResponse?> UpdateAsync(int id, GradeRequest request);
        Task<bool> DeleteAsync(int id);
    }
}

