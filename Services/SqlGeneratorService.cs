namespace DataSenseAPI.Services;

public class SqlGeneratorService : ISqlGeneratorService
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<SqlGeneratorService> _logger;

    public SqlGeneratorService(
        IOllamaService ollamaService,
        ILogger<SqlGeneratorService> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<string> GenerateSqlAsync(string naturalLanguageQuery, string schema)
    {
        var prompt = $@"You are a SQL query generator for Microsoft SQL Server.
Given a database schema and a natural language question, generate a valid, safe SQL SELECT query.

Database schema:
{schema}

Question: ""{naturalLanguageQuery}""

IMPORTANT RULES:
1. Generate ONLY SELECT queries (no INSERT, UPDATE, DELETE, DROP, TRUNCATE),
2. Use parameterized values or single quotes for string literals,
3. Use aggregation functions (COUNT, SUM, AVG, etc.) when appropriate,
4. Return ONLY the SQL query, no explanations or markdown formatting,
5. Use table and column names exactly as shown in the schema,
6. Do not use unnessecary JOINs, only use joins that are necessary for the given schema,
7. remember that there are no other tables in the database, only the ones in the schema,
8. check sql query again after generation for any syntax errors for the given schema and fix them,

Return the SQL query:";

        try
        {
            var response = await _ollamaService.QueryLLMAsync(prompt);
            
            // Clean up the response - remove any markdown formatting
            var sqlQuery = response.Trim();
            if (sqlQuery.StartsWith("```sql"))
            {
                sqlQuery = sqlQuery.Substring(6).Trim();
            }
            if (sqlQuery.StartsWith("```"))
            {
                sqlQuery = sqlQuery.Substring(3).Trim();
            }
            if (sqlQuery.EndsWith("```"))
            {
                sqlQuery = sqlQuery.Substring(0, sqlQuery.Length - 3).Trim();
            }
            
            return sqlQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL query");
            throw;
        }
    }
}

