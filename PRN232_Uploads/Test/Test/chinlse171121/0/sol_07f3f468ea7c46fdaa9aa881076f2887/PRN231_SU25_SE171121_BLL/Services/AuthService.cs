using Microsoft.Extensions.Configuration;
using PRN231_SU25_SE171121_BLL.Interfaces;
using PRN231_SU25_SE171121_DAL.Repositories;
using PRN231_SU25_SE171121_DAL.Helpers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PRN231_SU25_SE171121_DAL.Enum;

namespace PRN231_SU25_SE171121_BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<(string token, string role)?> AuthenticateAsync(string email, string password)
        {
            var account = await _unitOfWork.LeopardAccounts.GetAsync(a => a.Email == email && a.Password == password);
            if (account == null)
                return null;

            var roleEnum = (RoleEnum)(account.RoleId);
            var role = roleEnum.ToString().ToLower();

            if (!new[] { "administrator", "moderator", "developer", "member", "admin", "manager", "staff" }.Contains(role))
                return null;

            var token = JwtHelper.GenerateToken(account, role, _config);
            return (token, role);
        }
    }
}
