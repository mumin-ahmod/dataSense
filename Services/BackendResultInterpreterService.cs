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

            var prompt = $@"You are an assistant. You've been given data from the database and the original question.

Original Question: ""{request.OriginalQuery}""

Relevant data from database:
{resultsJson}

Your task:
1. Analyze the data thoroughly. Provide insights, summaries, or interpretations as appropriate.
2. Answer the original question based on the actual data provided.
3. If there are no results or the data is empty, give a relevant and close answer to the original question.
4. Be concise but informative (aim for 2-4 sentences).
5. If the results contain multiple rows, provide a meaningful summary or highlight key findings.

Provide your analysis and answer in natural language, and return ONLY the following JSON structure in your reply:
{{
    ""analysis"": ""..."",
    ""answer"": ""..."",
    ""summary"": ""...""
}}";


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

