using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRN232.Final.ExamEval.API.Hubs;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;
using PRN232.Final.ExamEval.Repositories.Repositories;
using PRN232.Final.ExamEval.Services.Extensions;
using PRN232.Final.ExamEval.Services.IServices;
using PRN232.Final.ExamEval.Services.Services;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace PRN232.Final.ExamEval.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ------------------------------ DB CONTEXT ------------------------------

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

            // ------------------------------ SERVICES ------------------------------

            builder.Services.AddControllers()
                .AddOData(options =>
                {
                    options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
                })
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    opts.JsonSerializerOptions.MaxDepth = 64;
                });
    
            builder.Services.AddEndpointsApiExplorer();
            
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Exam Eval API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: **{your token}**"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                c.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema
                {
                    Type = "string",
                    Format = "date",
                    Example = new Microsoft.OpenApi.Any.OpenApiString("2025-01-01")
                });
            });

            // ------------------------------ INTERFACES ------------------------------

            builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
            builder.Services.AddScoped<IServiceManager, ServiceManager>();
            builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
            builder.Services.AddScoped<ISemesterService, SemesterService>();
            builder.Services.AddScoped<IExamRepository, ExamRepository>();
            builder.Services.AddScoped<IExamService, ExamService>();
            builder.Services.AddScoped<IRubricRepository, RubricRepository>();
            builder.Services.AddScoped<IRubricService, RubricService>();
            builder.Services.AddScoped<IExaminerAssignmentRepository, ExaminerAssignmentRepository>();
            builder.Services.AddScoped<IExaminerAssignmentService, ExaminerAssignmentService>();
            builder.Services.AddScoped<IGradeRepository, GradeRepository>();
            builder.Services.AddScoped<IGradeService, GradeService>();
            builder.Services.AddScoped<ISubmissionForStudentRepository, SubmissionForStudentRepository>();
            builder.Services.AddScoped<ISubmissionForStudentService, SubmissionForStudentService>();


            // ------------------------------ MAPSTERS ------------------------------

            builder.Services.ConfigureMapsters();

            // ------------------------------ IDENTITY ------------------------------

            builder.Services.AddIdentity<User, Role>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            }) 
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders(); // this ensures UserManager, RoleManager, SignInManager are registered

            // ------------------------------ JWT ------------------------------

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            if (string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("JWT secret key is not configured.");

            builder.Services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true; // set false only in dev
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],

                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notification"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // ------------------------------ AUTHORIZATION POLICIES ------------------------------

            builder.Services.AddAuthorization(options =>
            {
                // Full access for admin + mod
                options.AddPolicy("FullAccess", policy =>
                    policy.RequireRole("administrator", "moderator"));

                // Read-only for all roles that have tokens
                options.AddPolicy("ReadOnly", policy =>
                    policy.RequireRole("administrator", "moderator", "examiner", "student"));
            });
            //-------------------------------SIGNALR + CORS------------------------------------
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyHeader()
                          .AllowAnyMethod()
                          .SetIsOriginAllowed(_ => true)
                          .AllowCredentials();
                });
            });
            builder.Services.AddSignalR();


            // ====================================================================================================
            // ------------------------------ APP PIPELINE ------------------------------

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await DbSeeder.SeedUsersAndRolesAsync(roleManager, userManager, context);
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/hubs/notification");
            app.MapControllers();

            app.Run();
        }
    }
}
