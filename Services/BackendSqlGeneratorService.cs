using System.Text;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

/// <summary>
/// Implementation of backend SQL generator that uses provided schema from SDK
/// </summary>
public class BackendSqlGeneratorService : IBackendSqlGeneratorService
{
    private readonly IOllamaService _ollamaService;
    private readonly ISqlSafetyValidator _safetyValidator;
    private readonly ILogger<BackendSqlGeneratorService> _logger;

    public BackendSqlGeneratorService(
        IOllamaService ollamaService,
        ISqlSafetyValidator safetyValidator,
        ILogger<BackendSqlGeneratorService> logger)
    {
        _ollamaService = ollamaService;
        _safetyValidator = safetyValidator;
        _logger = logger;
    }

    public async Task<string> GenerateSqlAsync(string naturalQuery, DatabaseSchema schema, string dbType = "sqlserver")
    {
        // Convert schema to text format for LLM
        var schemaText = FormatSchemaForLLM(schema);

        // Debug: Print schema and query to console
        Console.WriteLine("\n=== DEBUG: Generate SQL Called ===");
        Console.WriteLine($"Natural Query: \"{naturalQuery}\"");
        Console.WriteLine($"Database: {schema.DatabaseName}");
        Console.WriteLine($"Number of Tables: {schema.Tables.Count}");
        Console.WriteLine($"DB Type: {dbType}");
        Console.WriteLine("\nSchema Details:");
        Console.WriteLine(schemaText);
        Console.WriteLine("=== END DEBUG ===\n");

        var prompt = $@"You are a SQL query generator for {dbType.ToUpperInvariant()}.
Given a database schema and a natural language question, generate a valid, safe SQL SELECT query.

Database: {schema.DatabaseName}
Schema:
{schemaText}

Question: ""{naturalQuery}""

IMPORTANT RULES:
1. Generate ONLY SELECT queries (no INSERT, UPDATE, DELETE, DROP, TRUNCATE)
2. Use proper {dbType} syntax
3. Include all necessary JOINs based on foreign keys shown in the relationships
4. Use parameterized values or single quotes for string literals
5. Use aggregation functions (COUNT, SUM, AVG, etc.) when appropriate
6. Return ONLY the SQL query, no explanations or markdown formatting
7. Use table and column names exactly as shown in the schema
8. Be aware of NULL handling and use appropriate functions

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
            
            _logger.LogInformation($"Generated SQL for {dbType}: {sqlQuery}");
            
            // Step 2: Sanitize and validate safety before returning
            var sanitizedQuery = _safetyValidator.SanitizeQuery(sqlQuery);
            var isSafe = _safetyValidator.IsSafe(sanitizedQuery);
            
            if (!isSafe)
            {
                _logger.LogWarning($"Generated SQL failed safety validation. Attempting to fix: {sqlQuery}");
                
                // Step 3: Attempt to fix the query using LLM
                var fixedQuery = await VerifyAndFixQueryAsync(naturalQuery, schema, sanitizedQuery, dbType, schemaText);
                
                // Validate the fixed query
                var fixedSanitized = _safetyValidator.SanitizeQuery(fixedQuery);
                var fixedIsSafe = _safetyValidator.IsSafe(fixedSanitized);
                
                if (fixedIsSafe)
                {
                    _logger.LogInformation("Successfully fixed SQL query after safety validation");
                    return fixedSanitized;
                }
                else
                {
                    _logger.LogWarning("Fixed query still failed safety validation");
                    throw new InvalidOperationException("Generated SQL query contains dangerous operations and could not be fixed");
                }
            }
            
            return sanitizedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL query");
            throw;
        }
    }

    private string FormatSchemaForLLM(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {table.Schema}.{table.Name}");
            
            // List columns
            sb.AppendLine("  Columns:");
            foreach (var column in table.Columns)
            {
                var pkIndicator = column.IsPrimaryKey ? " (PK)" : "";
                var nullableIndicator = column.IsNullable ? " NULL" : " NOT NULL";
                var maxLength = column.MaxLength > 0 ? $"({column.MaxLength})" : "";
                
                sb.AppendLine($"    - {column.Name}: {column.DataType}{maxLength}{pkIndicator}{nullableIndicator}");
            }
            
            // List relationships
            if (table.Relationships.Any())
            {
                sb.AppendLine("  Relationships:");
                foreach (var rel in table.Relationships)
                {
                    sb.AppendLine($"    - {rel.ForeignKeyTable}.{rel.ForeignKeyColumn} -> {rel.PrimaryKeyTable}.{rel.PrimaryKeyColumn}");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private async Task<string> VerifyAndFixQueryAsync(string naturalQuery, DatabaseSchema schema, string sqlQuery, string dbType, string schemaText)
    {
        var verificationPrompt = $@"You are a SQL query verifier for {dbType.ToUpperInvariant()}.

Your task is to verify and fix a SQL query that failed safety validation.

Original Question: ""{naturalQuery}""

Generated SQL Query (REJECTED):
{sqlQuery}

Database Schema:
{schemaText}

Instructions:
1. The query was rejected because it contains dangerous operations or is not a SELECT statement
2. You MUST generate a valid, safe SELECT query that answers the original question
3. Ensure the query ONLY contains SELECT operations
4. Verify that all table names and column names exist in the schema
5. Fix any syntax errors or logical issues
6. Use proper {dbType} syntax

Return ONLY the corrected SQL query, no explanations or markdown formatting.";

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
            
            _logger.LogInformation("Verification fixed SQL query: {SqlQuery}", verifiedQuery);
            
            return verifiedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during verification, returning original query");
            throw;
        }
    }
}

