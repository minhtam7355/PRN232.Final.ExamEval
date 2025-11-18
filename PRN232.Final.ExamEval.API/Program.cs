using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Repositories.IRepositories;
using PRN232.Final.ExamEval.Repositories.Persistence;
using PRN232.Final.ExamEval.Repositories.Repositories;
using PRN232.Final.ExamEval.Services.Extensions;
using PRN232.Final.ExamEval.Services.IServices;
using PRN232.Final.ExamEval.Services.Services;
using System.Text;
using System.Text.Json.Serialization;
using SubmitionsChecker;

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

            // ------------------------------ Cloudinary Config ------------------------------
            var cloudConfig = builder.Configuration.GetSection("Cloudinary");
            var account = new Account(
                cloudConfig["CloudName"],
                cloudConfig["ApiKey"],
                cloudConfig["ApiSecret"]
            );

            builder.Services.AddSingleton(new Cloudinary(account));


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
            builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
            builder.Services.AddScoped<ISubmissionImageRepository, SubmissionImageRepository>();
            builder.Services.AddScoped<ISubmissionService, SubmissionService>();
            builder.Services.AddScoped<ISubmissionImageService, SubmissionImageService>();


            // Register submission processor (MOSS disabled temporarily)
            builder.Services.AddScoped<SubmissionProcessor>();

            // ------------------------------ MAPSTERS ------------------------------

            builder.Services.ConfigureMapsters();

            // ------------------------------ CORS ------------------------------
            builder.Services.AddCors();

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
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors(builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyOrigin();
            });

            app.MapControllers();

            app.Run();
        }
    }
}
