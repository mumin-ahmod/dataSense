namespace DataSenseAPI.Models;

public class QueryRequest
{
    public string NaturalLanguageQuery { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
}

