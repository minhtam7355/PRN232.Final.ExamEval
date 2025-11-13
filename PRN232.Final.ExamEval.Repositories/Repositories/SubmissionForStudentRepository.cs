using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public class SubmissionForStudentRepository : ISubmissionForStudentRepository
    {
        private readonly AppDbContext _context;

        public SubmissionForStudentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Submission>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Submission?> GetByIdAsync(int id)
        {
            return await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubmissionId == id);
        }

        public async Task<Submission> CreateAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }
    }
}
