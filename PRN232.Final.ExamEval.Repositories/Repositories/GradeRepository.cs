using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;

namespace PRN232.Final.ExamEval.Repositories.Repositories
{
    public class GradeRepository : IGradeRepository
    {
        private readonly AppDbContext _context;

        public GradeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Grade>> GetAllAsync() =>
            await _context.Grades.AsNoTracking().ToListAsync();

        public async Task<Grade?> GetByIdAsync(int id) =>
            await _context.Grades.AsNoTracking().FirstOrDefaultAsync(g => g.GradeId == id);

        public async Task<Grade> CreateAsync(Grade grade)
        {
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return grade;
        }

        public async Task<Grade?> UpdateAsync(int id, Grade grade)
        {
            var existing = await _context.Grades.FindAsync(id);
            if (existing == null) return null;

            existing.Score = grade.Score;
            existing.Comment = grade.Comment;
            existing.GradedAt = DateTime.UtcNow;
            existing.SubmissionId = grade.SubmissionId;
            existing.ExaminerId = grade.ExaminerId;

            _context.Grades.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return false;
            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

