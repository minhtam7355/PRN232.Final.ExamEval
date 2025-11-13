using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Repository.Model;
using Service.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PRN232_SU25_SE185118.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILeopardAccountService _accountService;
        private readonly IConfiguration _config;

        public AuthController(ILeopardAccountService accountService, IConfiguration config)
        {
            _accountService = accountService;
            _config = config;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _accountService.Authenticate(request.Email, request.Password);

            if (user == null)
                return Unauthorized("Invalid credentials or inactive account.");

            if (!_accountService.IsTokenAllowed(user))
                return Forbid("No token issued for this role.");

            var roleName = _accountService.GetRoleName(user);
            if (roleName == null)
                return Forbid("Role not allowed.");

            var claims = new[]
            {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:ExpireMinutes"])),
                signingCredentials: creds
            );

            return Ok(new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Role = roleName
            });
        }
    }

}
