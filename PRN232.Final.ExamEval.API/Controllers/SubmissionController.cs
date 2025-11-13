using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Persistence;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Examiner")]
    public class SubmissionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubmissionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var submissions = await _context.Submissions
                .AsNoTracking()
                .Select(s => new {
                    s.SubmissionId,
                    s.FilePath,
                    s.SubmittedAt,
                    s.HasViolation,
                    s.ExamId,
                    s.StudentId
                })
                .ToListAsync();

            return Ok(submissions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _context.Submissions
                .AsNoTracking()
                .Select(s => new {
                    s.SubmissionId,
                    s.FilePath,
                    s.SubmittedAt,
                    s.HasViolation,
                    s.ExamId,
                    s.StudentId
                })
                .FirstOrDefaultAsync(s => s.SubmissionId == id);

            return s == null ? NotFound() : Ok(s);
        }
    }
}
