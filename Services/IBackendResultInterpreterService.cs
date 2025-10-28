using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

/// <summary>
/// Service for interpreting query results in the backend
/// </summary>
public interface IBackendResultInterpreterService
{
    /// <summary>
    /// Interprets query results and provides structured interpretation
    /// </summary>
    /// <param name="request">Request containing original query, SQL, and results</param>
    /// <returns>Structured interpretation with analysis, answer, and summary</returns>
    Task<InterpretationData> InterpretResultsAsync(InterpretResultsRequest request);
}

