namespace DataSenseAPI.Models;

public class QueryResponse
{
    public string Action { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public List<string> Conditions { get; set; } = new();
    public string? RawJson { get; set; }
}

