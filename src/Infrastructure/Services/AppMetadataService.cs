using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class AppMetadataService : IAppMetadataService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<AppMetadataService> _logger;

    public AppMetadataService(IRedisService redisService, ILogger<AppMetadataService> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    public async Task SaveAppMetadataAsync(string userId, AppMetadata metadata)
    {
        metadata.UserId = userId;
        metadata.UpdatedAt = DateTime.UtcNow;
        await _redisService.SaveAppMetadataAsync(userId, metadata);
        _logger.LogInformation("App metadata saved for user: {UserId}", userId);
    }

    public async Task<AppMetadata?> GetAppMetadataAsync(string userId)
    {
        return await _redisService.GetAppMetadataAsync(userId);
    }

    public async Task<List<string>> GenerateWelcomeSuggestionsAsync(DatabaseSchema? schema)
    {
        var suggestions = new List<string>();

        if (schema != null && schema.Tables.Any())
        {
            // Generate suggestions based on available tables
            var tableNames = schema.Tables.Select(t => t.Name).Take(5).ToList();
            
            foreach (var tableName in tableNames)
            {
                suggestions.Add($"Tell me about {tableName}");
                suggestions.Add($"Show me data from {tableName}");
            }
        }

        // Add generic suggestions if we don't have enough
        var genericSuggestions = new[]
        {
            "Help me with my data",
            "What can I query?",
            "Show me examples"
        };

        suggestions.AddRange(genericSuggestions.Take(3 - suggestions.Count));

        return suggestions.Take(3).ToList();
    }
}

