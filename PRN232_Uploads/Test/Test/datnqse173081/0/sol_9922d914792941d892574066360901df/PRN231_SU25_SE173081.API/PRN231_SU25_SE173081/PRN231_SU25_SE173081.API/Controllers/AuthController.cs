using BLL.DTOs;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace PRN231_SU25_SE173081.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(AccountService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await service.LoginAsync(request);
                return Ok(response);
            }
            catch (Exception e)
            {
                return BadRequest(new ErrorReponses.ErrorResponse("HB40001", "Missing/invalid input"));
            }
        }
    }
}
