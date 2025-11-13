using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface ISubmissionForStudentService
    {
        Task<IEnumerable<SubmissionResponse>> GetByStudentIdAsync(Guid studentId);
        Task<SubmissionResponse?> GetByIdAsync(int id);
        Task<SubmissionResponse> CreateAsync(SubmissionRequest request, Guid studentId);
    }
}
