using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Abstractions;

public interface IBackendSqlGeneratorService
{
    Task<string> GenerateSqlAsync(string naturalQuery, DatabaseSchema schema, string dbType = "sqlserver");
}

public interface IBackendResultInterpreterService
{
    Task<InterpretationData> InterpretResultsAsync(InterpretResultsRequest request);
}

public interface ISqlSafetyValidator
{
    bool IsSafe(string sqlQuery);
    string SanitizeQuery(string sqlQuery);
}

public interface IOllamaService
{
    Task<string> QueryLLMAsync(string prompt);
}


