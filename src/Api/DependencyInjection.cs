using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Security.Claims;

namespace DataSenseAPI.Api;

/// <summary>
/// Extension methods for registering API layer dependencies.
/// This includes authentication, authorization, CORS, Swagger, and API-specific middleware.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers API layer services including authentication, authorization, CORS, and Swagger.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Register Controllers and API Explorer
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Configure JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] ?? Guid.NewGuid().ToString();
        var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

        // Store JWT secret in configuration for TokenService
        configuration["Jwt:Secret"] = jwtSecret;
        configuration["Jwt:Issuer"] = configuration["Jwt:Issuer"] ?? "datasense";
        configuration["Jwt:Audience"] = configuration["Jwt:Audience"] ?? "datasense-api";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                ValidateIssuer = true,
                ValidIssuer = "datasense",
                ValidateAudience = true,
                ValidAudience = "datasense-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                // Ensure role-based authorization uses the correct claim type
                RoleClaimType = ClaimTypes.Role
            };
        });

        // Configure Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("SystemAdminOnly", policy => policy.RequireRole("SystemAdmin"));
            // Align with controllers using [Authorize(Policy = "SystemAdmin")]
            options.AddPolicy("SystemAdmin", policy => policy.RequireRole("SystemAdmin"));
            options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
        });

        // Configure CORS
        services.AddCors(options =>
        {
            if (environment.IsDevelopment())
            {
                // More permissive CORS for development
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin =>
                        {
                            if (string.IsNullOrEmpty(origin))
                                return false;

                            try
                            {
                                // Allow all localhost, 127.0.0.1, and 0.0.0.0 origins in development
                                var uri = new Uri(origin);
                                return uri.Host == "localhost"
                                    || uri.Host == "127.0.0.1"
                                    || uri.Host == "0.0.0.0"
                                    || uri.Host == "[::1]"; // IPv6 localhost
                            }
                            catch
                            {
                                return false;
                            }
                        })
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("*");
                });
            }
            else
            {
                // Restricted CORS for production
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                              "http://localhost:4200",
                              "http://localhost:5050",
                              "http://127.0.0.1:4200",
                              "http://127.0.0.1:5050")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            }
        });

        return services;
    }
}

