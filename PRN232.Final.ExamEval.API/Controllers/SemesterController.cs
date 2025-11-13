using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Moderator")]
    public class SemesterController : ControllerBase
    {
        private readonly ISemesterService _semesterService;

        public SemesterController(ISemesterService semesterService)
        {
            _semesterService = semesterService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var semesters = await _semesterService.GetAllAsync();
            return Ok(semesters);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var semester = await _semesterService.GetByIdAsync(id);
            return semester == null ? NotFound() : Ok(semester);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SemesterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");

            var created = await _semesterService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.SemesterId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] SemesterRequest request)
        {
            var updated = await _semesterService.UpdateAsync(id, request);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _semesterService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
