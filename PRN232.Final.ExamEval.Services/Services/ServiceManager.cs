using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IAuthService> authService;

        public ServiceManager(IRepositoryManager repositoryManager, IMapper mapper, UserManager<User> userManager, RoleManager<Role> roleManager, IConfiguration configuration)
        {
            authService = new Lazy<IAuthService>(() => new AuthService(userManager, configuration));
        }

        public IAuthService AuthService => authService.Value;

    }
}
