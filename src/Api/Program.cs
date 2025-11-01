using DataSenseAPI.Application;
using DataSenseAPI.Infrastructure;
using DataSenseAPI.Infrastructure.AppDb;
using DataSenseAPI.Infrastructure.Services;
using DataSenseAPI.Api;
using DataSenseAPI.Api.Middleware;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
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

// Register services by layer (Clean Architecture)
builder.Services.AddApplication();                      // Application layer (MediatR, handlers)
builder.Services.AddInfrastructure(builder.Configuration); // Infrastructure layer (repositories, services, database, identity)
builder.Services.AddApiServices(builder.Configuration, builder.Environment); // API layer (authentication, authorization, CORS, Swagger)

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
await RoleSeeder.SeedRolesAsync(app.Services);
await RoleSeeder.SeedSubscriptionPlansAsync(app.Services);

app.UseAuthentication();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseMiddleware<RequestTrackingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();


