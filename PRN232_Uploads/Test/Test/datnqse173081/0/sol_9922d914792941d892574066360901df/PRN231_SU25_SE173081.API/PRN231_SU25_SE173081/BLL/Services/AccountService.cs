using BLL.DTOs;
using DLL.Enum;
using DLL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class AccountService(AccountRepo accountRepo, JWTService service)
    {

        public async Task<LoginResponse> LoginAsync(LoginRequest loginRequest)
        {
            var account = await accountRepo.Login(loginRequest.Email, loginRequest.Password);
            if (account == null)
            {
                throw new Exception("Invalid username and password");
            }
            var response = new LoginResponse()
            {
                Role = account.RoleId.ToString(),
                Token = service.GenerateToken(account)
            };
            return response;

        }
    }
}
