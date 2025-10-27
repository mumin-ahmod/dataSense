using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

/// <summary>
/// Service for interpreting query results in the backend
/// </summary>
public interface IBackendResultInterpreterService
{
    /// <summary>
    /// Interprets query results and provides natural language summary
    /// </summary>
    /// <param name="request">Request containing original query, SQL, and results</param>
    /// <returns>Natural language interpretation of results</returns>
    Task<string> InterpretResultsAsync(InterpretResultsRequest request);
}

