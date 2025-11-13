using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Administrator,Moderator")]
    public class RubricController : ControllerBase
    {
        private readonly IRubricService _rubricService;

        public RubricController(IRubricService rubricService)
        {
            _rubricService = rubricService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var rubrics = await _rubricService.GetAllAsync();
            return Ok(rubrics);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var rubric = await _rubricService.GetByIdAsync(id);
            return rubric == null ? NotFound() : Ok(rubric);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RubricRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Criteria))
                return BadRequest("Criteria is required.");

            var created = await _rubricService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.RubricId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RubricRequest request)
        {
            var updated = await _rubricService.UpdateAsync(id, request);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _rubricService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
