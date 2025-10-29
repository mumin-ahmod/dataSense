using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class QueryDetectionService : IQueryDetectionService
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<QueryDetectionService> _logger;

    public QueryDetectionService(IOllamaService ollamaService, ILogger<QueryDetectionService> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<bool> NeedsQueryExecutionAsync(string message, DatabaseSchema? schema)
    {
        try
        {
            // Keywords that suggest database query needs
            var queryKeywords = new[] { "show", "list", "get", "find", "search", "count", "select", "how many", "what are", "which" };
            var hasQueryKeywords = queryKeywords.Any(keyword => message.ToLower().Contains(keyword.ToLower()));

            // If schema is provided and message contains query keywords, use LLM to determine
            if (schema != null && hasQueryKeywords)
            {
                var prompt = $@"Analyze if the following user message requires querying a database to answer accurately. 
Consider the available database schema: {string.Join(", ", schema.Tables.Select(t => t.Name))}

User message: ""{message}""

Respond with only 'YES' if database query is needed, or 'NO' if it can be answered without querying.
Be conservative - only say YES if the message clearly requires database data.";

                var response = await _ollamaService.QueryLLMAsync(prompt);
                return response.Trim().ToUpper().StartsWith("YES");
            }

            return hasQueryKeywords && schema != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting if query execution is needed");
            // Default to false to avoid unnecessary queries
            return false;
        }
    }
}

