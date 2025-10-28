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
        // Step 1: Generate initial SQL query
        var prompt = $@"You are a SQL query generator for Microsoft SQL Server.
Given a database schema and a natural language question, generate a valid, safe SQL SELECT query.

Database schema:
{schema}

Question: ""{naturalLanguageQuery}""

IMPORTANT RULES:
1. Generate ONLY SELECT queries (no INSERT, UPDATE, DELETE, DROP, TRUNCATE),
2. remember that there are no other tables in the database, only the ones in the schema,
3. Do not use unnessecary JOINs, only use joins that are necessary from only the given schema,
4. Use parameterized values or single quotes for string literals,
5. Use aggregation functions when appropriate,
6. Return ONLY the SQL query, no explanations or markdown formatting,
7. Use table and column names exactly as shown in the schema,

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
            
            _logger.LogInformation("Initial SQL query generated: {SqlQuery}", sqlQuery);
            
            // Step 2: Verify and fix the query
            var verifiedQuery = await VerifyAndFixQueryAsync(naturalLanguageQuery, schema, sqlQuery);
            
            return verifiedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL query");
            throw;
        }
    }

    private async Task<string> VerifyAndFixQueryAsync(string naturalLanguageQuery, string schema, string sqlQuery)
    {
        var verificationPrompt = $@"You are a SQL query verifier for Microsoft SQL Server.

Your task is to verify and fix a SQL query against the given schema and question.

Generated SQL Query:
{sqlQuery}

Instructions:
1. Check if this SQL query is syntactically correct for the given schema
2. Verify that all table names and column names exist in the schema
3. Check for any syntax errors or logical issues
4. If there are any issues, provide the corrected SQL query
5. If the query is correct, return it as is

Database Schema:
{schema},

Return ONLY the verified/corrected SQL query, no explanations or markdown formatting.";

        try
        {
            var verifiedResponse = await _ollamaService.QueryLLMAsync(verificationPrompt);
            
            // Clean up the response
            var verifiedQuery = verifiedResponse.Trim();
            if (verifiedQuery.StartsWith("```sql"))
            {
                verifiedQuery = verifiedQuery.Substring(6).Trim();
            }
            if (verifiedQuery.StartsWith("```"))
            {
                verifiedQuery = verifiedQuery.Substring(3).Trim();
            }
            if (verifiedQuery.EndsWith("```"))
            {
                verifiedQuery = verifiedQuery.Substring(0, verifiedQuery.Length - 3).Trim();
            }
            
            _logger.LogInformation("Verified SQL query: {SqlQuery}", verifiedQuery);
            
            return verifiedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during verification, returning original query");
            // Return the original query if verification fails
            return sqlQuery;
        }
    }
}

