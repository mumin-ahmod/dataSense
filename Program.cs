using DataSenseAPI.Services;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register services
builder.Services.AddSingleton<ISchemaCacheService, SchemaCacheService>();
builder.Services.AddHttpClient<OllamaService>();

builder.Services.AddScoped<IOllamaService, OllamaService>();
builder.Services.AddScoped<IDatabaseSchemaReader, DatabaseSchemaReader>();
builder.Services.AddScoped<IQueryParserService, QueryParserService>();

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

// Initialize schema cache on startup
try
{
    var schemaCache = app.Services.GetRequiredService<ISchemaCacheService>();
    await schemaCache.RefreshSchemaAsync();
    Console.WriteLine("✓ Database schema loaded successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Warning: Could not load database schema: {ex.Message}");
    Console.WriteLine("   The API will work, but without schema information.");
}

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

