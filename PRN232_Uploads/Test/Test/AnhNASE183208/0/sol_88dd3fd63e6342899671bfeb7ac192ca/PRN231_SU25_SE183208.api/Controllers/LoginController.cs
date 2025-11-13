using Business.dtos;
using DataAccess.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Business.dtos;
using Business.Services;

namespace PRN231_SU25_SE183208.api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class LoginController : Controller
    {
        private readonly AccountService _accountService;

        public LoginController(AccountService viroCureUserService)
        {
            _accountService = viroCureUserService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid request data.");

            try
            {
                LoginResponseDTO respDto = await _accountService.Login(loginDto.email, loginDto.password);
                return Ok(respDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }
}
