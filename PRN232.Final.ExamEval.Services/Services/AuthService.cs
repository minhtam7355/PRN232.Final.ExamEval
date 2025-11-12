using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Requests;
using PRN232.Final.ExamEval.Repositories.RequestsAndResponses.Responses;
using PRN232.Final.ExamEval.Services.IServices;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> userManager;
        private readonly IConfiguration configuration;

        public AuthService(UserManager<User> userManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.configuration = configuration;
        }

        #region ValidateUser
        public async Task<User?> ValidateUser(UserForAuthenticationRequest userForAuth, CancellationToken ct = default)
        {
            var user = await userManager.FindByEmailAsync(userForAuth.Email);

            if (user == null || !await userManager.CheckPasswordAsync(user, userForAuth.Password))
                return null;

            // Reset failed attempts & update last login
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.UpdateAsync(user);

            return user;
        }
        #endregion

        #region CreateToken
        public async Task<TokenResponse> CreateToken(User user, bool setRefreshExpiry, CancellationToken ct = default)
        {
            var jwtSettings = configuration.GetSection("Jwt");

            var signingCredentials = GetSigningCredentials();
            var claims = await GetClaims(user);
            var token = GenerateTokenOptions(signingCredentials, claims);

            var refreshToken = await GenerateAndStoreRefreshToken(user, setRefreshExpiry, ct);

            return new TokenResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                AccessTokenExpiry = token.ValidTo,
                RefreshTokenExpiry = user.RefreshTokenExpiryTime
            };
        }
        #endregion

        #region RefreshToken
        public async Task<TokenResponse> RefreshToken(string accessToken, string refreshToken, CancellationToken ct = default)
        {
            var principal = GetPrincipalFromExpiredToken(accessToken);
            var username = principal.Identity!.Name!;
            var user = await userManager.Users
                        .Include(u => u.UserProfile)
                        .FirstOrDefaultAsync(u => u.UserName == username, ct);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new SecurityTokenException("Invalid or expired refresh token");

            return await CreateToken(user, setRefreshExpiry: false, ct);
        }
        #endregion

        #region GetUserByToken
        public async Task<UserForReturnResponse> GetUserByToken(string jwt, CancellationToken ct = default)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var userId = Guid.Parse(token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

            var user = await userManager.Users
                        .Include(u => u.UserProfile)
                        .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user == null)
                throw new Exception("User not found");

            var roles = await userManager.GetRolesAsync(user);

            return new UserForReturnResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Fullname = user.UserProfile?.Fullname,
                Avatar = user.UserProfile?.Avatar,
                Bio = user.UserProfile?.Bio,
                Birthday = user.UserProfile?.Birthday,
                StudentID = user.UserProfile?.StudentID,
                EmployeeID = user.UserProfile?.EmployeeID,
                Roles = roles.ToList()
            };
        }
        #endregion

        #region Helpers
        private SigningCredentials GetSigningCredentials()
        {
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]);
            return new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var expiryDays = Convert.ToDouble(jwtSettings["ExpiryInDays"]);

            return new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: signingCredentials
            );
        }

        private async Task<string> GenerateAndStoreRefreshToken(User user, bool setRefreshExpiry, CancellationToken ct)
        {
            user.RefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            if (setRefreshExpiry)
            {
                var expiryDays = Convert.ToDouble(configuration["Jwt:RefreshTokenExpiryInDays"]);
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(expiryDays);
            }

            await userManager.UpdateAsync(user);
            return user.RefreshToken;
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = false, // important for expired tokens
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
        #endregion
    }
}
