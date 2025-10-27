namespace DataSenseAPI.Services;

public interface IOllamaService
{
    Task<string> QueryLLMAsync(string prompt);
}

