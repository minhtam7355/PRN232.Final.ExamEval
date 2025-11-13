using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PRN232.Final.ExamEval.API.Hubs;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Examiner")]
    public class GradeController : ControllerBase
    {
        private readonly IGradeService _service;
        private readonly IHubContext<NotificationHub> _hubContext;

        public GradeController(IGradeService service, IHubContext<NotificationHub> hubContext)
        {
            _service = service;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var grades = await _service.GetAllAsync();
            return Ok(grades);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var grade = await _service.GetByIdAsync(id);
            return grade == null ? NotFound() : Ok(grade);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GradeRequest request)
        {
            var created = await _service.CreateAsync(request);

            // 🟢 Phát notification real-time
            await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"🧾 Submission {request.SubmissionId} has been graded with score {request.Score}.");

            return CreatedAtAction(nameof(GetById), new { id = created.GradeId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] GradeRequest request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated == null) return NotFound();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"✏️ Grade {id} has been updated to score {request.Score}.");

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"🗑️ Grade {id} has been deleted.");

            return NoContent();
        }
    }
}
