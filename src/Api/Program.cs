using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Infrastructure.Services;
using DataSenseAPI.Infrastructure.Repositories;
using DataSenseAPI.Infrastructure.AppDb;
using DataSenseAPI.Api.Middleware;
using DataSenseAPI.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Explicitly bind URLs to avoid macOS AirPlay conflicts and IPv6-only binding quirks
var configuredUrls = builder.Configuration["Urls"];
if (string.IsNullOrWhiteSpace(configuredUrls))
{
    configuredUrls = "http://0.0.0.0:5050;http://127.0.0.1:5050;http://localhost:5050";
}
builder.WebHost.UseUrls(configuredUrls);

// Allow overriding DB connection string via environment variable
var envDbConnection = Environment.GetEnvironmentVariable("DATASENSE_DB_CONNECTION");
if (!string.IsNullOrWhiteSpace(envDbConnection))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = envDbConnection;
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR and application services
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DataSenseAPI.Application.Commands.GenerateSql.GenerateSqlCommand).Assembly));

// Infrastructure services
builder.Services.AddHttpClient<OllamaService>();
// Hosted services are singletons; ensure IOllamaService can be injected safely
builder.Services.AddSingleton<IOllamaService, OllamaService>();
builder.Services.AddScoped<ISqlSafetyValidator, SqlSafetyValidator>();
builder.Services.AddScoped<IBackendSqlGeneratorService, BackendSqlGeneratorService>();
builder.Services.AddScoped<IBackendResultInterpreterService, BackendResultInterpreterService>();

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IRedisService, RedisService>();

// Kafka Service
builder.Services.AddSingleton<IKafkaService, KafkaService>();

// New Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IQueryDetectionService, QueryDetectionService>();
builder.Services.AddScoped<IAppMetadataService, AppMetadataService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// Email services
builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

// Background Services
builder.Services.AddHostedService<KafkaOllamaConsumer>();
builder.Services.AddHostedService<UserLockoutUnlockService>();
builder.Services.AddHostedService<EmailSenderBackgroundService>();

// Dapper Repositories
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<IRequestLogRepository, RequestLogRepository>();
builder.Services.AddScoped<IPricingRecordRepository, PricingRecordRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
builder.Services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
builder.Services.AddScoped<IUsageRequestRepository, UsageRequestRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

// Database and Identity (for authentication only)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy => policy.RequireRole("SystemAdmin"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Guid.NewGuid().ToString(); // Generate if not set
var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

// Store JWT secret in configuration for TokenService
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Jwt:Issuer"] = builder.Configuration["Jwt:Issuer"] ?? "datasense";
builder.Configuration["Jwt:Audience"] = builder.Configuration["Jwt:Audience"] ?? "datasense-api";

builder.Services.AddAuthentication(options =>
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
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
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

var app = builder.Build();

Console.WriteLine("✓ DataSense Backend API ready");
Console.WriteLine("  Endpoints: /api/v1/backend/generate-sql, /api/v1/backend/interpret-results");
Console.WriteLine("  Chat endpoints: /api/v1/backend/welcome-suggestions, /api/v1/backend/start-conversation, /api/v1/backend/send-message");

// If URLs are pre-configured (e.g., ASPNETCORE_URLS or command-line), print them immediately
var preConfiguredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(preConfiguredUrls))
{
    Console.WriteLine($"  Listening on: {preConfiguredUrls}");
}

// Ensure database is created and migrations are applied before seeding/serving
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Print which server and addresses the app is running on
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var environmentName = app.Environment.EnvironmentName;
        var server = app.Services.GetService<Microsoft.AspNetCore.Hosting.Server.IServer>();
        var addressesFeature = server?.Features.Get<IServerAddressesFeature>();
        var addresses = addressesFeature?.Addresses;

        if (addresses != null && addresses.Any())
        {
            foreach (var address in addresses)
            {
                Console.WriteLine($"✓ Hosting: {address} | Environment: {environmentName}");
            }
        }
        else
        {
            // Fallback to configured URLs if addresses are not yet populated
            var configured = string.Join(", ", app.Urls);
            Console.WriteLine($"✓ Hosting: {configured} | Environment: {environmentName}");
        }
    }
    catch
    {
        // Intentionally ignore any exceptions here to avoid impacting startup
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataSense API v1");
        c.RoutePrefix = string.Empty;
    });

    // Provide a helpful hint for Swagger location if URLs are known
    if (!string.IsNullOrWhiteSpace(preConfiguredUrls))
    {
        foreach (var baseUrl in preConfiguredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            Console.WriteLine($"  Swagger UI: {baseUrl.TrimEnd('/')}/swagger");
        }
    }
}

// Middleware order is important
app.UseRouting();
app.UseCors("AllowAll");

// Seed roles and subscription plans
await DataSenseAPI.Infrastructure.Services.RoleSeeder.SeedRolesAsync(app.Services);
await DataSenseAPI.Infrastructure.Services.RoleSeeder.SeedSubscriptionPlansAsync(app.Services);

app.UseAuthentication();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RequestTrackingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();


