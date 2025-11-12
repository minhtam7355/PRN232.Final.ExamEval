using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Services.IServices;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IServiceManager serviceManager;

        public AuthController(IServiceManager serviceManager)
        {
            this.serviceManager = serviceManager;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserForAuthenticationRequest userForAuth, CancellationToken ct)
        {
            var user = await serviceManager.AuthService.ValidateUser(userForAuth, ct);

            if (user == null)
                return Unauthorized();

            var tokenResponse = await serviceManager.AuthService.CreateToken(user, true, ct);

            // Set refresh token cookie
            Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = tokenResponse.RefreshTokenExpiry
            });

            return Ok(tokenResponse.AccessToken);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(CancellationToken ct)
        {
            var accessToken = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(accessToken))
                return BadRequest("Access token is missing");

            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                return Unauthorized("Refresh token not found");

            try
            {
                var tokenResponse = await serviceManager.AuthService.RefreshToken(accessToken, refreshToken, ct);

                Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken!, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = tokenResponse.RefreshTokenExpiry
                });

                return Ok(tokenResponse.AccessToken);
            }
            catch (SecurityTokenException)
            {
                return Unauthorized("Invalid or expired refresh token");
            }
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Split(" ")[1];
            var user = await serviceManager.AuthService.GetUserByToken(token, ct);
            return Ok(user);
        }

    }
}
