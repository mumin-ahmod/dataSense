using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Infrastructure.Services;
using DataSenseAPI.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

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

// Database and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();


