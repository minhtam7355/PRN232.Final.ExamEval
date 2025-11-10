using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Persistence
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid>
    {
        private readonly IPasswordHasher<User> passwordHasher;

        public AppDbContext(DbContextOptions<AppDbContext> options, IPasswordHasher<User> passwordHasher) : base(options)
        {
            this.passwordHasher = passwordHasher;
        }

    }
}