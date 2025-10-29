using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Infrastructure.Services;
using DataSenseAPI.Infrastructure.AppDb;
using DataSenseAPI.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR and application services
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DataSenseAPI.Application.Commands.GenerateSql.GenerateSqlCommand).Assembly));

// Infrastructure services
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<ISqlSafetyValidator, SqlSafetyValidator>();
builder.Services.AddScoped<IBackendSqlGeneratorService, BackendSqlGeneratorService>();
builder.Services.AddScoped<IBackendResultInterpreterService, BackendResultInterpreterService>();

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddScoped<IRedisService, RedisService>();

// Kafka Service
builder.Services.AddSingleton<IKafkaService, KafkaService>();

// New Services
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IQueryDetectionService, QueryDetectionService>();
builder.Services.AddScoped<IAppMetadataService, AppMetadataService>();

// Kafka Consumer Background Service
builder.Services.AddHostedService<KafkaOllamaConsumer>();

// Database and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? Guid.NewGuid().ToString(); // Generate if not set
var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

Console.WriteLine("✓ DataSense Backend API ready");
Console.WriteLine("  Endpoints: /api/v1/backend/generate-sql, /api/v1/backend/interpret-results");
Console.WriteLine("  Chat endpoints: /api/v1/backend/welcome-suggestions, /api/v1/backend/start-conversation, /api/v1/backend/send-message");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order is important
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RequestTrackingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();


