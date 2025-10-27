using System.Text.Json;

namespace DataSenseAPI.Services;

public class ResultAnalyzerService : IResultAnalyzerService
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<ResultAnalyzerService> _logger;

    public ResultAnalyzerService(
        IOllamaService ollamaService,
        ILogger<ResultAnalyzerService> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<string> AnalyzeResultsAsync(string originalQuery, object queryResults)
    {
        try
        {
            // Serialize the results to JSON for the LLM
            var resultsJson = JsonSerializer.Serialize(queryResults, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });

            var prompt = $@"You are a data analyst assistant. You've been given a database query result and the original question.

Original Question: ""{originalQuery}""

Query Results (JSON):
{resultsJson}

Your task:
1. Analyze the data
2. Answer the original question based on the actual data provided
3. Provide insights, summaries, or interpretations as appropriate
4. If there are no results, explain why and what it means
5. Be concise but informative

Provide your analysis and answer in natural language:";

            _logger.LogInformation("Sending query results to Ollama for analysis");

            var response = await _ollamaService.QueryLLMAsync(prompt);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query results");
            throw;
        }
    }
}

