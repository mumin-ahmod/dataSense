using Microsoft.Extensions.DependencyInjection;

namespace DataSenseAPI.Services;

public class SchemaCacheService : ISchemaCacheService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchemaCacheService> _logger;
    private string _cachedSchema = string.Empty;
    private bool _schemaLoaded = false;

    public bool IsSchemaLoaded => _schemaLoaded;

    public SchemaCacheService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SchemaCacheService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetSchema()
    {
        if (!_schemaLoaded && !string.IsNullOrEmpty(_cachedSchema))
        {
            _schemaLoaded = true;
        }
        return _cachedSchema;
    }

    public async Task RefreshSchemaAsync()
    {
        try
        {
            var connectionString = GetConnectionString();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("No database connection string configured");
                _cachedSchema = "No database schema available - please configure connection string in appsettings.json";
                _schemaLoaded = true;
                return;
            }

            _logger.LogInformation("Reading database schema...");
            
            // Create a scope to get the scoped service
            using var scope = _serviceProvider.CreateScope();
            var schemaReader = scope.ServiceProvider.GetRequiredService<IDatabaseSchemaReader>();
            
            _cachedSchema = await schemaReader.GetSchemaAsync(connectionString);
            _schemaLoaded = true;
            _logger.LogInformation("Database schema loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading database schema");
            _cachedSchema = $"Error loading schema: {ex.Message}";
            _schemaLoaded = true;
        }
    }

    private string? GetConnectionString()
    {
        // Try to get from environment variable first
        var envConnection = Environment.GetEnvironmentVariable("DBCONNECTION");
        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            _logger.LogInformation("Using connection string from environment variable");
            return envConnection;
        }

        // Get from appsettings.json
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Try alternative names
            connectionString = _configuration.GetConnectionString("ConnectionString");
        }

        return connectionString;
    }
}

