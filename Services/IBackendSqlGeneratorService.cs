using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

/// <summary>
/// Service for generating SQL from natural language queries in the backend
/// Uses schema provided by the SDK instead of fetching from database
/// </summary>
public interface IBackendSqlGeneratorService
{
    /// <summary>
    /// Generates SQL from natural language query using provided schema
    /// </summary>
    /// <param name="naturalQuery">Natural language query</param>
    /// <param name="schema">Database schema provided by SDK</param>
    /// <param name="dbType">Target database type</param>
    /// <returns>Generated SQL query</returns>
    Task<string> GenerateSqlAsync(string naturalQuery, DatabaseSchema schema, string dbType = "sqlserver");
}

