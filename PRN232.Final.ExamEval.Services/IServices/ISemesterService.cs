using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface ISemesterService
    {
        Task<IEnumerable<SemesterResponse>> GetAllAsync();
        Task<SemesterResponse?> GetByIdAsync(int id);
        Task<SemesterResponse> CreateAsync(SemesterRequest request);
        Task<SemesterResponse?> UpdateAsync(int id, SemesterRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
