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
    public class ExaminerAssignmentRepository : IExaminerAssignmentRepository
    {
        private readonly AppDbContext _context;

        public ExaminerAssignmentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExaminerAssignment>> GetAllAsync()
        {
            return await _context.ExaminerAssignments
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ExaminerAssignment?> GetByIdAsync(int id)
        {
            return await _context.ExaminerAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ExaminerAssignmentId == id);
        }

        public async Task<ExaminerAssignment> CreateAsync(ExaminerAssignment entity)
        {
            _context.ExaminerAssignments.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ExaminerAssignment?> UpdateAsync(int id, ExaminerAssignment entity)
        {
            var existing = await _context.ExaminerAssignments.FindAsync(id);
            if (existing == null) return null;

            existing.ExaminerId = entity.ExaminerId;
            existing.ExamId = entity.ExamId;

            _context.ExaminerAssignments.Update(existing);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.ExaminerAssignments.FindAsync(id);
            if (entity == null) return false;

            _context.ExaminerAssignments.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
