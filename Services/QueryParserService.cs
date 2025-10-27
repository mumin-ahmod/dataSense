using System.Text.Json;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

public class QueryParserService : IQueryParserService
{
    private readonly ISqlGeneratorService _sqlGenerator;
    private readonly ISqlSafetyValidator _safetyValidator;
    private readonly IQueryExecutor _queryExecutor;
    private readonly ISchemaCacheService _schemaCache;
    private readonly ILogger<QueryParserService> _logger;

    public QueryParserService(
        ISqlGeneratorService sqlGenerator,
        ISqlSafetyValidator safetyValidator,
        IQueryExecutor queryExecutor,
        ISchemaCacheService schemaCache,
        ILogger<QueryParserService> logger)
    {
        _sqlGenerator = sqlGenerator;
        _safetyValidator = safetyValidator;
        _queryExecutor = queryExecutor;
        _schemaCache = schemaCache;
        _logger = logger;
    }

    public async Task<QueryResponse> ParseQueryAsync(string naturalLanguageQuery)
    {
        try
        {
            _logger.LogInformation($"Processing query: {naturalLanguageQuery}");

            // Get cached database schema
            var schema = _schemaCache.GetSchema();

            // Generate SQL query
            var sqlQuery = await _sqlGenerator.GenerateSqlAsync(naturalLanguageQuery, schema);
            _logger.LogInformation($"Generated SQL: {sqlQuery}");

            // Validate SQL safety
            var sanitizedQuery = _safetyValidator.SanitizeQuery(sqlQuery);
            
            if (!_safetyValidator.IsSafe(sanitizedQuery))
            {
                _logger.LogWarning("Generated SQL query is not safe");
                return new QueryResponse
                {
                    SqlQuery = sqlQuery,
                    IsValid = false,
                    ErrorMessage = "Generated SQL query contains dangerous operations or is not a SELECT statement"
                };
            }

            // Execute the query
            var results = await _queryExecutor.ExecuteQueryAsync(sanitizedQuery);
            
            _logger.LogInformation("Query executed successfully");

            return new QueryResponse
            {
                SqlQuery = sanitizedQuery,
                Results = results,
                IsValid = true,
                ParsedStructure = $"Query for: {naturalLanguageQuery}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing and executing query");
            return new QueryResponse
            {
                SqlQuery = string.Empty,
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

}

