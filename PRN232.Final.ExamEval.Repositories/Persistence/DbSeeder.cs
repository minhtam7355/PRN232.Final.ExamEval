using Microsoft.AspNetCore.Identity;
using PRN232.Final.ExamEval.Repositories.Entities;

namespace PRN232.Final.ExamEval.Repositories.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedUsersAndRolesAsync(
            RoleManager<Role> roleManager,
            UserManager<User> userManager,
            AppDbContext context)
        {
            // ------------------------- Seed Roles -------------------------
            var roles = new[]
            {
                new Role { Id = Guid.Parse("6f7b3f0c-3f54-4fb8-a215-33cd496c3be7"), Name = "Administrator", NormalizedName = "ADMINISTRATOR" },
                new Role { Id = Guid.Parse("7211a346-6e23-431c-a6bd-2f02aa5de68a"), Name = "Moderator", NormalizedName = "MODERATOR" },
                new Role { Id = Guid.Parse("b86a5b00-0393-4524-9f56-fa7ca800e79c"), Name = "Examiner", NormalizedName = "EXAMINER" },
                new Role { Id = Guid.Parse("51999f00-0f63-4236-8c81-94c43fcf7586"), Name = "Student", NormalizedName = "STUDENT" }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                    await roleManager.CreateAsync(role);
            }

            // ------------------------- Seed Users -------------------------
            var users = new[]
            {
                new { Id = "e5d8947f-6794-42b6-ba67-201f366128b8", Username = "administrator", Password = "administrator@1", Role = "Administrator" },
                new { Id = "3fe77296-fdb3-4d71-8b99-ef8380c32037", Username = "moderator", Password = "moderator@1", Role = "Moderator" },
                new { Id = "23879117-e09e-40f1-b78f-1493d81baf49", Username = "examiner1", Password = "examiner1", Role = "Examiner" },
                new { Id = "91b106fa-7b95-480f-a12a-0e0303454332", Username = "examiner2", Password = "examiner2", Role = "Examiner" },
                new { Id = "537f05fd-120c-40b0-b2ec-639756f866ab", Username = "examiner3", Password = "examiner3", Role = "Examiner" },
                new { Id = "293191b7-f7b2-4f28-8857-5afa96866a2f", Username = "student1", Password = "student1", Role = "Student" },
                new { Id = "34670beb-a794-4419-adf8-0465eea22a78", Username = "student2", Password = "student2", Role = "Student" },
                new { Id = "c25dc5ef-4e98-421e-90d3-7eb76ba269fe", Username = "student3", Password = "student3", Role = "Student" }
            };

            foreach (var u in users)
            {
                var user = await userManager.FindByIdAsync(u.Id);
                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.Parse(u.Id),
                        UserName = u.Username,
                        NormalizedUserName = u.Username.ToUpper(),
                        Email = $"{u.Username}@gmail.com",
                        NormalizedEmail = $"{u.Username}@gmail.com".ToUpper(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        IsActive = true,
                        JoinedDate = DateTime.UtcNow
                    };

                    await userManager.CreateAsync(user, u.Password);
                }

                if (!await userManager.IsInRoleAsync(user, u.Role))
                    await userManager.AddToRoleAsync(user, u.Role);
            }

            // ------------------------- Seed UserProfiles -------------------------
            var profiles = new[]
            {
                new UserProfile { UserId = Guid.Parse("e5d8947f-6794-42b6-ba67-201f366128b8"), Fullname = "Administrator", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), EmployeeID = "EMP2025001" },
                new UserProfile { UserId = Guid.Parse("3fe77296-fdb3-4d71-8b99-ef8380c32037"), Fullname = "Moderator", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), EmployeeID = "EMP2025002" },
                new UserProfile { UserId = Guid.Parse("23879117-e09e-40f1-b78f-1493d81baf49"), Fullname = "Examiner1", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), EmployeeID = "EMP2025003" },
                new UserProfile { UserId = Guid.Parse("91b106fa-7b95-480f-a12a-0e0303454332"), Fullname = "Examiner2", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), EmployeeID = "EMP2025004" },
                new UserProfile { UserId = Guid.Parse("537f05fd-120c-40b0-b2ec-639756f866ab"), Fullname = "Examiner3", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), EmployeeID = "EMP2025005" },
                new UserProfile { UserId = Guid.Parse("293191b7-f7b2-4f28-8857-5afa96866a2f"), Fullname = "Student1", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), StudentID = "STU2025001" },
                new UserProfile { UserId = Guid.Parse("34670beb-a794-4419-adf8-0465eea22a78"), Fullname = "Student2", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), StudentID = "STU2025002" },
                new UserProfile { UserId = Guid.Parse("c25dc5ef-4e98-421e-90d3-7eb76ba269fe"), Fullname = "Student3", Avatar = "https://...placeholder...", Birthday = DateOnly.Parse("2002-01-23"), StudentID = "STU2025003" }
            };

            foreach (var profile in profiles)
            {
                if (!context.UserProfiles.Any(up => up.UserId == profile.UserId))
                    context.UserProfiles.Add(profile);
            }

            // ============================================================
            // ========== SEED SUBJECTS (NO ID, DB GENERATES ID) ===========
            // ============================================================

            if (!context.Subjects.Any())
            {
                context.Subjects.AddRange(new[]
                {
                    new Subject { Name = "C# Programming", Description = "Object-oriented programming in C#" },
                    new Subject { Name = "Database Systems", Description = "SQL, normalization, ERD" },
                    new Subject { Name = "Web Development with Java", Description = "Servlets, JSP, Spring" },
                    new Subject { Name = "Cloud Computing", Description = "AWS, Azure fundamentals" }
                });
            }

            // ============================================================
            // ========== SEED SEMESTERS (NO ID, DB GENERATES ID) ==========
            // ============================================================

            if (!context.Semesters.Any())
            {
                context.Semesters.AddRange(new[]
                {
                    new Semester { Name = "Fall 2025", StartDate = DateTime.Parse("2025-08-20"), EndDate = DateTime.Parse("2025-12-20") },
                    new Semester { Name = "Spring 2026", StartDate = DateTime.Parse("2026-01-05"), EndDate = DateTime.Parse("2026-05-20") }
                });
            }

            await context.SaveChangesAsync();  // SAVE BEFORE SEEDING EXAMS

            // ============================================================
            // ========== GET GENERATED IDs FOR FK USE =====================
            // ============================================================

            var cs = context.Subjects.First(s => s.Name == "C# Programming").SubjectId;
            var db = context.Subjects.First(s => s.Name == "Database Systems").SubjectId;
            var java = context.Subjects.First(s => s.Name == "Web Development with Java").SubjectId;
            var cloud = context.Subjects.First(s => s.Name == "Cloud Computing").SubjectId;

            var fall = context.Semesters.First(s => s.Name == "Fall 2025").SemesterId;
            var spring = context.Semesters.First(s => s.Name == "Spring 2026").SemesterId;

            // ============================================================
            // ===================== SEED EXAMS ============================
            // ============================================================

            if (!context.Exams.Any())
            {
                context.Exams.AddRange(new[]
                {
                    new Exam { Name = "C# OOP Midterm", ExamDate = DateTime.Parse("2025-10-10"), SubjectId = cs, SemesterId = fall },
                    new Exam { Name = "Database Final", ExamDate = DateTime.Parse("2025-12-15"), SubjectId = db, SemesterId = fall },
                    new Exam { Name = "Java Web Assignment", ExamDate = DateTime.Parse("2026-03-12"), SubjectId = java, SemesterId = spring },
                    new Exam { Name = "Cloud Computing Final", ExamDate = DateTime.Parse("2026-05-10"), SubjectId = cloud, SemesterId = spring }
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
