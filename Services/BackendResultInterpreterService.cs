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

    public async Task<string> InterpretResultsAsync(InterpretResultsRequest request)
    {
        try
        {
            // Serialize the results to JSON for the LLM
            var resultsJson = JsonSerializer.Serialize(request.Results, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });

            var prompt = $@"You are a data analyst assistant. You've been given a database query result and the original question.

Original Question: ""{request.OriginalQuery}""

SQL Query Executed:
```sql
{request.SqlQuery}
```

Query Results:
{resultsJson}

Your task:
1. Analyze the data thoroughly
2. Answer the original question based on the actual data provided
3. Provide insights, summaries, or interpretations as appropriate
4. If there are no results or empty data, explain why and what it means
5. Be concise but informative (aim for 2-4 sentences)
6. If the results contain multiple rows, provide a meaningful summary or highlight key findings

Provide your analysis and answer in natural language:";

            _logger.LogInformation("Interpreting query results for backend");

            var response = await _ollamaService.QueryLLMAsync(prompt);

            return response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interpreting query results");
            throw;
        }
    }
}

