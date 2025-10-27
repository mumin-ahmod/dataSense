using System.Text.Json;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

public class QueryParserService : IQueryParserService
{
    private readonly IOllamaService _ollamaService;
    private readonly ISchemaCacheService _schemaCache;
    private readonly ILogger<QueryParserService> _logger;

    public QueryParserService(
        IOllamaService ollamaService,
        ISchemaCacheService schemaCache,
        ILogger<QueryParserService> logger)
    {
        _ollamaService = ollamaService;
        _schemaCache = schemaCache;
        _logger = logger;
    }

    public async Task<QueryResponse> ParseQueryAsync(string naturalLanguageQuery)
    {
        try
        {
            // Get cached database schema
            var schema = _schemaCache.GetSchema();

            // Create the prompt for Ollama
            var prompt = $@"You are a database query interpreter.
Given a natural-language question and database schema, identify:
- Action (e.g., SUM(HoursLogged), COUNT(*))
- Table (main table)
- Conditions (filters)

Return only valid JSON, no explanation.

Schema:
{schema}

Question:
{naturalLanguageQuery}";

            // Query Ollama
            var response = await _ollamaService.QueryLLMAsync(prompt);

            // Parse the response
            var parsedResponse = ParseLLMResponse(response);

            return parsedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query");
            return new QueryResponse
            {
                Action = "ERROR",
                Table = "Unknown",
                Conditions = new List<string> { $"Error: {ex.Message}" }
            };
        }
    }

    private QueryResponse ParseLLMResponse(string response)
    {
        try
        {
            // Try to parse JSON response
            var jsonDocument = JsonDocument.Parse(response);
            var root = jsonDocument.RootElement;

            var queryResponse = new QueryResponse();

            if (root.TryGetProperty("Action", out var action))
            {
                queryResponse.Action = action.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("Table", out var table))
            {
                queryResponse.Table = table.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("Conditions", out var conditions))
            {
                if (conditions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var condition in conditions.EnumerateArray())
                    {
                        queryResponse.Conditions.Add(condition.GetString() ?? string.Empty);
                    }
                }
            }

            queryResponse.RawJson = response;

            return queryResponse;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON, returning raw response");
            
            // Fallback: try to extract JSON from the response if it contains JSON
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return ParseLLMResponse(jsonContent);
            }

            // Return raw response as fallback
            return new QueryResponse
            {
                Action = "PARSE_ERROR",
                Table = "Unknown",
                Conditions = new List<string>(),
                RawJson = response
            };
        }
    }
}

