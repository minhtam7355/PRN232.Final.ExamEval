using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using PRN231_SU25_SE171121_BLL.Interfaces;
using PRN231_SU25_SE171121_BLL.Response;

namespace PRN231_SU25_SE171121.api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = ModelState.Values.SelectMany(v => v.Errors)
                                                    .FirstOrDefault()?.ErrorMessage ?? "Invalid input";

                return BadRequest(new ErrorResponse
                {
                    ErrorCode = "HB40001",
                    Message = errorMessage
                });
            }

            try
            {
                var result = await _authService.AuthenticateAsync(request.Email, request.Password);

                if (result == null)
                {
                    return Unauthorized(new ErrorResponse
                    {
                        ErrorCode = "HB40101",
                        Message = "Invalid email or password"
                    });
                }

                return Ok(new
                {
                    token = result.Value.token,
                    role = result.Value.role
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse
                {
                    ErrorCode = "HB50001",
                    Message = "Internal server error"
                });
            }
        }
    }
}
