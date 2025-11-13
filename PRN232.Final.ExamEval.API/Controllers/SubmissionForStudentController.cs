using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Services.IServices;
using System.Security.Claims;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Student")]
    public class SubmissionForStudetnController : ControllerBase
    {
        private readonly ISubmissionForStudentService _service;

        public SubmissionForStudetnController(ISubmissionForStudentService service)
        {
            _service = service;
        }

        // 🟢 Lấy ID sinh viên từ JWT token
        private Guid GetStudentIdFromToken()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value;
            return Guid.Parse(userId);
        }

        // 🟡 Upload bài thi
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubmissionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FilePath))
                return BadRequest("FilePath is required.");

            var studentId = GetStudentIdFromToken();
            var created = await _service.CreateAsync(request, studentId);
            return CreatedAtAction(nameof(GetById), new { id = created.SubmissionId }, created);
        }

        // 🟢 Xem toàn bộ bài nộp của chính mình
        [HttpGet]
        public async Task<IActionResult> GetMySubmissions()
        {
            var studentId = GetStudentIdFromToken();
            var submissions = await _service.GetByStudentIdAsync(studentId);
            return Ok(submissions);
        }

        // 🟢 Xem chi tiết bài nộp (chỉ khi là của mình)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var studentId = GetStudentIdFromToken();
            var submission = await _service.GetByIdAsync(id);

            if (submission == null) return NotFound();
            if (submission.StudentId != studentId) return Forbid();

            return Ok(submission);
        }
    }
}
