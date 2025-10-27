namespace DataSenseAPI.Services;

public interface IResultAnalyzerService
{
    Task<string> AnalyzeResultsAsync(string originalQuery, object queryResults);
}

