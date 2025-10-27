using DataSenseAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services for backend API (SDK integration only)
builder.Services.AddHttpClient<OllamaService>();

// Core services for backend functionality
builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<ISqlSafetyValidator, SqlSafetyValidator>();

// Backend services (for SDK integration)
builder.Services.AddScoped<IBackendSqlGeneratorService, BackendSqlGeneratorService>();
builder.Services.AddScoped<IBackendResultInterpreterService, BackendResultInterpreterService>();

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

// Note: Schema is provided by SDK clients, not loaded from database
// Connection string configuration is reserved for future request logging
Console.WriteLine("✓ DataSense Backend API ready");
Console.WriteLine("  Endpoints: /api/v1/backend/generate-sql, /api/v1/backend/interpret-results");
Console.WriteLine("  Connection string is reserved for request logging (to be implemented)");

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

