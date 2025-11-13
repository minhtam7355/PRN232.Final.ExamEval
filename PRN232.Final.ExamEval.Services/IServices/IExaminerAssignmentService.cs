using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IExaminerAssignmentService
    {
        Task<IEnumerable<ExaminerAssignmentResponse>> GetAllAsync();
        Task<ExaminerAssignmentResponse?> GetByIdAsync(int id);
        Task<ExaminerAssignmentResponse> CreateAsync(ExaminerAssignmentRequest request);
        Task<ExaminerAssignmentResponse?> UpdateAsync(int id, ExaminerAssignmentRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
