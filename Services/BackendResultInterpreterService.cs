using System.Text.Json;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

public class BackendResultInterpreterService : IBackendResultInterpreterService
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<BackendResultInterpreterService> _logger;

    public BackendResultInterpreterService(
        IOllamaService ollamaService,
        ILogger<BackendResultInterpreterService> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<InterpretationData> InterpretResultsAsync(InterpretResultsRequest request)
    {
        try
        {
            // Serialize the results to JSON for the LLM
            var resultsJson = JsonSerializer.Serialize(request.Results, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });

            var prompt = $@"You are an assistant. You've been given data from the database and the original question.

Original Question: ""{request.OriginalQuery}""

Relevant data pulled using this query: {request.SqlQuery} from database:
{resultsJson}

YOUR TASK:
Analyze the data and answer the original question. Provide three things:
1. Analysis: What the data shows (2-4 sentences)
2. Answer: Direct answer to the original question (1-2 sentences)
3. Summary: Brief summary of key findings (1 sentence)

CRITICAL OUTPUT FORMAT:
You MUST respond with ONLY valid, complete JSON. No markdown, no code blocks, no explanations before or after. The JSON must be complete with proper closing braces. Respond with ONLY this exact structure:

{{""analysis"":""text here"",""answer"":""text here"",""summary"":""text here""}}";


            _logger.LogInformation("Interpreting query results for backend");

            var response = await _ollamaService.QueryLLMAsync(prompt);

             _logger.LogInformation("Interpretation response: {Response}", response);

            var trimmedResponse = response.Trim();
            
            // Sanitize JSON response - remove markdown code blocks if present
            trimmedResponse = SanitizeJsonResponse(trimmedResponse);
            
            // Parse the JSON response
            InterpretationData? interpretation = null;
            try
            {
                interpretation = JsonSerializer.Deserialize<InterpretationData>(trimmedResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to parse JSON response from LLM, attempting recovery");
                
                // Try to recover incomplete JSON by adding missing closing brace
                var recoveredJson = AttemptJsonRecovery(trimmedResponse);
                if (recoveredJson != null)
                {
                    try
                    {
                        interpretation = JsonSerializer.Deserialize<InterpretationData>(recoveredJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        _logger.LogInformation("Successfully recovered JSON");
                    }
                    catch (JsonException ex2)
                    {
                        _logger.LogWarning(ex2, "Recovery also failed");
                    }
                }
            }

            // Fallback: if parsing failed, return the full response in summary
            if (interpretation == null)
            {
                _logger.LogInformation("Using fallback: returning full LLM response as summary");
                interpretation = new InterpretationData
                {
                    Analysis = string.Empty,
                    Answer = string.Empty,
                    Summary = trimmedResponse
                };
            }

            return interpretation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interpreting query results");
            throw;
        }
    }

    private string SanitizeJsonResponse(string response)
    {
        var sanitized = response.Trim();
        
        // Remove markdown code blocks
        if (sanitized.StartsWith("```json"))
        {
            sanitized = sanitized.Substring(7).Trim();
        }
        else if (sanitized.StartsWith("```"))
        {
            sanitized = sanitized.Substring(3).Trim();
        }
        
        if (sanitized.EndsWith("```"))
        {
            sanitized = sanitized.Substring(0, sanitized.Length - 3).Trim();
        }
        
        return sanitized;
    }

    private string? AttemptJsonRecovery(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
            
        var trimmed = json.Trim();
        
        // Count opening and closing braces
        var openBraces = trimmed.Count(c => c == '{');
        var closeBraces = trimmed.Count(c => c == '}');
        
        // If missing closing braces, add them
        if (openBraces > closeBraces)
        {
            var missing = openBraces - closeBraces;
            _logger.LogInformation("Attempting to recover JSON by adding {Count} closing brace(s)", missing);
            return trimmed + new string('}', missing);
        }
        
        return null;
    }
}

