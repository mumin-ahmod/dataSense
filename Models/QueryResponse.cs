namespace DataSenseAPI.Models;

public class QueryResponse
{
    public string SqlQuery { get; set; } = string.Empty;
    public object? Results { get; set; }
    public string? Analysis { get; set; } // For API Set 2 - Ollama's interpretation
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ParsedStructure { get; set; } // Legacy field for debugging
}

