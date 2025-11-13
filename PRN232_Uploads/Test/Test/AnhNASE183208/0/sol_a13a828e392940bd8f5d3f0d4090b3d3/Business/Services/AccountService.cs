using Business.dtos;
using DataAccess.Models;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services
{
    public class AccountService
    {
        private LeopardProfileRepo _repo;
        private JwtService _jwtService;
        private IConfiguration _configuration;

        public AccountService(LeopardProfileRepo repo, JwtService jwtService, IConfiguration configuration)
        {
            _repo = repo;
            _jwtService = jwtService;
            _configuration = configuration;
        }
        public async Task<LoginResponseDTO> Login(string email, string password)
        {
            LeopardAccount account = await _repo.GetUserAccount(email, password);
            if (account == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }
            String secretKey = _configuration["JwtSettings:SecretKey"];

            string role = account.RoleId switch
            {
                1 => "admin",
                2 => "patient",
                3 => "doctor"
            };

            String token = _jwtService.GenerateToken(account.AccountId, secretKey, role);

            UserRespDto userResp = new UserRespDto();
            userResp.Id = account.AccountId;
            userResp.Role = role;
            userResp.Email = account.Email;
            LoginResponseDTO loginResp = new LoginResponseDTO();
            loginResp.Role = account.RoleId;
            loginResp.Token = token;
            return loginResp;
        }
    }
}
