using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PRN232.Final.ExamEval.Repositories.Persistence;
using PRN232.Final.ExamEval.Services.Extensions;
using System;
using System.Text;
using System.Text.Json.Serialization;

namespace PRN232.Final.ExamEval.API
{
    public class Program
    {
        public static void Main(string[] args)
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

            //            builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            // ------------------------------ MAPSTERS ------------------------------

            builder.Services.ConfigureMapsters();

            // ------------------------------ JWT ------------------------------

            var jwtSection = builder.Configuration.GetSection("Jwt");
            var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "default_secret_key");

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection["Issuer"],

                        ValidateAudience = true,
                        ValidAudience = jwtSection["Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
