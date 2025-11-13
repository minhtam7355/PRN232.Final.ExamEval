using Microsoft.AspNetCore.Mvc;
using PRN231_SU25_SE171121_BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using PRN231_SU25_SE171121_BLL.Response;

namespace PRN231_SU25_SE171121.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeopardProfileController : ControllerBase
    {
        private readonly ILeopardProfileService _service;

        public LeopardProfileController(ILeopardProfileService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "administrator,moderator,developer,member")]
        public async Task<IActionResult> GetAll()
        {
          try
            {
             var result = await _service.GetAllAsync();
            return Ok(result);
            }
            catch
            {
            return StatusCode(500, new ErrorResponse { ErrorCode = "HB50001", Message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "administrator,moderator")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new ErrorResponse { ErrorCode = "HB40401", Message = "Resource not found" });

                return Ok();
            }
            catch
            {
                return StatusCode(500, new ErrorResponse { ErrorCode = "HB50001", Message = "Internal server error" });
            }
        }
    }
}
