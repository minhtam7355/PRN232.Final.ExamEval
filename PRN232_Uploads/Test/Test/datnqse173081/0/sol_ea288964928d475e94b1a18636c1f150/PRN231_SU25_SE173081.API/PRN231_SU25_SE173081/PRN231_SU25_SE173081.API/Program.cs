
using BLL.Services;
using DLL;
using DLL.Repositories;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using Microsoft.OpenApi.Models;
using PRN231_SU25_SE173081.API.ErrorReponses;
using System.Text;
using System.Text.Json;

namespace PRN231_SU25_SE173081.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddControllers();
            builder.Services.AddDbContext<Su25leopardDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            builder
            .Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddJwtBearer(
              BearerTokenDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["Jwt:Issuer"],
                            ValidAudience = builder.Configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                            ),
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";

                            var result = JsonSerializer.Serialize(
                                new ErrorResponse("HB40101", "Token missing or invalid"),
                                new JsonSerializerOptions()
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                }
                            );

                            return context.Response.WriteAsync(result);
                        },

                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";

                            var result = JsonSerializer.Serialize(
                                new ErrorResponse("HB40301", "Permission denied"),
                                new JsonSerializerOptions()
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                }
                            );

                            return context.Response.WriteAsync(result);
                        },
                    };
                }
    );

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errorMessage = context
                        .ModelState.Values.SelectMany(v => v.Errors)
                        .FirstOrDefault()
                        ?.ErrorMessage;

                    return new BadRequestObjectResult(
                        new ErrorResponse("HB40001", errorMessage ?? "Invalid request")
                    );
                };
            });


            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
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
            });
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<JWTService>();
            builder.Services.AddScoped<AccountService>();
            builder.Services.AddScoped<AccountRepo>();
            builder.Services.AddScoped<ProfileRepo>();
            builder.Services.AddScoped<ProfileService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
