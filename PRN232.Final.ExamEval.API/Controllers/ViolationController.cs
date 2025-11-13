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
    public class ViolationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ViolationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var violations = await _context.Violations
                .AsNoTracking()
                .Select(v => new {
                    v.ViolationId,
                    v.Type,
                    v.Description,
                    v.IsConfirmed,
                    v.SubmissionId
                })
                .ToListAsync();

            return Ok(violations);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var v = await _context.Violations
                .AsNoTracking()
                .Select(v => new {
                    v.ViolationId,
                    v.Type,
                    v.Description,
                    v.IsConfirmed,
                    v.SubmissionId
                })
                .FirstOrDefaultAsync(v => v.ViolationId == id);

            return v == null ? NotFound() : Ok(v);
        }
    }
}
