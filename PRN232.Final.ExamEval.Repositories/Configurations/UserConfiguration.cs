using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PRN232.Final.ExamEval.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Repositories.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        private readonly IPasswordHasher<User> passwordHasher;

        public UserConfiguration(IPasswordHasher<User> passwordHasher)
        {
            this.passwordHasher = passwordHasher;
        }
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("User");

            builder.Property(u => u.IsActive);
            builder.Property(u => u.JoinedDate);
            builder.Property(u => u.RefreshToken);
            builder.Property(u => u.RefreshTokenExpiryTime);

            builder.HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            var administrator = new User
            {
                Id = Guid.Parse("e5d8947f-6794-42b6-ba67-201f366128b8"),
                UserName = "administrator",
                NormalizedUserName = "ADMINISTRATOR",
                Email = "administrator@gmail.com",
                NormalizedEmail = "ADMINISTRATOR@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, "administrator@1");

            var moderator = new User
            {
                Id = Guid.Parse("3fe77296-fdb3-4d71-8b99-ef8380c32037"),
                UserName = "moderator",
                NormalizedUserName = "MODERATOR",
                Email = "moderator@gmail.com",
                NormalizedEmail = "MODERATOR@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            moderator.PasswordHash = passwordHasher.HashPassword(moderator, "moderator@1");

            var examiner1 = new User
            {
                Id = Guid.Parse("23879117-e09e-40f1-b78f-1493d81baf49"),
                UserName = "examiner1",
                NormalizedUserName = "EXAMINER1",
                Email = "examiner1@gmail.com",
                NormalizedEmail = "EXAMINER1@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            examiner1.PasswordHash = passwordHasher.HashPassword(examiner1, "examiner1");

            var examiner2 = new User
            {
                Id = Guid.Parse("91b106fa-7b95-480f-a12a-0e0303454332"),
                UserName = "examiner2",
                NormalizedUserName = "EXAMINER2",
                Email = "examiner2@gmail.com",
                NormalizedEmail = "EXAMINER2@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            examiner2.PasswordHash = passwordHasher.HashPassword(examiner2, "examiner2");

            var examiner3 = new User
            {
                Id = Guid.Parse("537f05fd-120c-40b0-b2ec-639756f866ab"),
                UserName = "examiner3",
                NormalizedUserName = "EXAMINER3",
                Email = "examiner3@gmail.com",
                NormalizedEmail = "EXAMINER3@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            examiner3.PasswordHash = passwordHasher.HashPassword(examiner3, "examiner3");

            var student1 = new User
            {
                Id = Guid.Parse("293191b7-f7b2-4f28-8857-5afa96866a2f"),
                UserName = "student1",
                NormalizedUserName = "STUDENT1",
                Email = "student1@gmail.com",
                NormalizedEmail = "STUDENT1@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            student1.PasswordHash = passwordHasher.HashPassword(student1, "student1");

            var student2 = new User
            {
                Id = Guid.Parse("34670beb-a794-4419-adf8-0465eea22a78"),
                UserName = "student2",
                NormalizedUserName = "STUDENT2",
                Email = "student2@gmail.com",
                NormalizedEmail = "STUDENT2@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            student2.PasswordHash = passwordHasher.HashPassword(student2, "student2");

            var student3 = new User
            {
                Id = Guid.Parse("c25dc5ef-4e98-421e-90d3-7eb76ba269fe"),
                UserName = "student3",
                NormalizedUserName = "STUDENT3",
                Email = "student3@gmail.com",
                NormalizedEmail = "STUDENT3@GMAIL.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                IsActive = true,
                JoinedDate = DateTime.UtcNow
            };
            student3.PasswordHash = passwordHasher.HashPassword(student3, "student3");

            builder.HasData(administrator, moderator, examiner1, examiner2, examiner3, student1, student2, student3);
        }
    }
}
