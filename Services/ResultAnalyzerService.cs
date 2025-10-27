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
//need to change this prompt like: instead of data analyst, take input like: personal assistant, data analyst, shopping expert etc.
//change the prompt bellow to be role specific.
            var prompt = $@"You are an assistant. You've been given a data and the original question.

Original Question: ""{originalQuery}""

Relevant Data from the database (JSON):
{resultsJson}

Your task:
1. Answer the original question based on the actual data provided
2. Provide insights, summaries, or interpretations as appropriate
3. If there are no results, explain why and what it means
4. Be concise but informative, do not use technical terms, keep it simple and easy to understand.
5. Do not use database terms, keep it simple and easy to understand.

Provide your response and answer in natural language:";

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

