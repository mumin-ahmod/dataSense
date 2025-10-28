using System.Text;
using System.Text.Json;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private const string OllamaBaseUrl = "http://localhost:11434";

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> QueryLLMAsync(string prompt)
    {
        var request = new
        {
            model = "llama3.1:8b",
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{OllamaBaseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (jsonResponse.TryGetProperty("response", out var responseText))
            {
                return responseText.GetString() ?? string.Empty;
            }

            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Ollama");
            throw;
        }
    }
}

