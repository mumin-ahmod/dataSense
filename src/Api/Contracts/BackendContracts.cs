using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Contracts;

public class GenerateSqlRequest
{
    public string NaturalQuery { get; set; } = string.Empty;
    public DatabaseSchema Schema { get; set; } = new();
    public string DbType { get; set; } = "sqlserver";
}

public class GenerateSqlResponse
{
    public string SqlQuery { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class InterpretResultsRequest
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public object Results { get; set; } = new();
}

public class InterpretResultsResponse
{
    public Domain.Models.InterpretationData? Interpretation { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}


