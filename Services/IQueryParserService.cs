using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

public interface IQueryParserService
{
    Task<QueryResponse> ParseQueryAsync(string naturalLanguageQuery);
}

