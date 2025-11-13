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
    public class RubricRepository : IRubricRepository
    {
        private readonly AppDbContext _context;

        public RubricRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Rubric>> GetAllAsync()
        {
            return await _context.Rubrics.AsNoTracking().ToListAsync();
        }

        public async Task<Rubric?> GetByIdAsync(int id)
        {
            return await _context.Rubrics.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RubricId == id);
        }

        public async Task<Rubric> CreateAsync(Rubric rubric)
        {
            _context.Rubrics.Add(rubric);
            await _context.SaveChangesAsync();
            return rubric;
        }

        public async Task<Rubric?> UpdateAsync(int id, Rubric rubric)
        {
            var existing = await _context.Rubrics.FindAsync(id);
            if (existing == null) return null;

            existing.Criteria = rubric.Criteria;
            existing.MaxScore = rubric.MaxScore;
            existing.ExamId = rubric.ExamId;

            _context.Rubrics.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rubric = await _context.Rubrics.FindAsync(id);
            if (rubric == null) return false;

            _context.Rubrics.Remove(rubric);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
