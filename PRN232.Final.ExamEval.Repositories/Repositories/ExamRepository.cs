using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly AppDbContext _context;

        public ExamRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Exam>> GetAllAsync()
        {
            return await _context.Exams.AsNoTracking().ToListAsync();
        }

        public async Task<Exam?> GetByIdAsync(int id)
        {
            return await _context.Exams.AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExamId == id);
        }

        public async Task<Exam> CreateAsync(Exam exam)
        {
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            return exam;
        }

        public async Task<Exam?> UpdateAsync(int id, Exam exam)
        {
            var existing = await _context.Exams.FindAsync(id);
            if (existing == null) return null;

            existing.Name = exam.Name;
            existing.ExamDate = exam.ExamDate;
            existing.SubjectId = exam.SubjectId;
            existing.SemesterId = exam.SemesterId;

            _context.Exams.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return false;

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
