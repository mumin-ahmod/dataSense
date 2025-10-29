using System.Text;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Domain.Models;
using DataSenseAPI.Application.Abstractions;

namespace DataSenseAPI.Infrastructure.Services;

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
        var schemaText = FormatSchemaForLLM(schema);

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
            var sqlQuery = CleanupCodeBlocks(response.Trim());
            _logger.LogInformation($"Generated SQL for {dbType}: {sqlQuery}");

            _logger.LogInformation("Verifying and fixing generated SQL query");
            var verifiedQuery = await VerifyAndFixQueryAsync(naturalQuery, schema, sqlQuery, dbType, schemaText);

            var sanitizedQuery = _safetyValidator.SanitizeQuery(verifiedQuery);
            var isSafe = _safetyValidator.IsSafe(sanitizedQuery);
            if (!isSafe)
            {
                _logger.LogWarning("Verified SQL query failed safety validation");
                throw new InvalidOperationException("Generated SQL query contains dangerous operations and could not be fixed");
            }

            _logger.LogInformation("SQL query verified and passed safety validation");
            return sanitizedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL query");
            throw;
        }
    }

    private static string CleanupCodeBlocks(string text)
    {
        var t = text;
        if (t.StartsWith("```sql")) t = t.Substring(6).Trim();
        if (t.StartsWith("```") ) t = t.Substring(3).Trim();
        if (t.EndsWith("```")) t = t.Substring(0, t.Length - 3).Trim();
        return t;
    }

    private string FormatSchemaForLLM(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {table.Schema}.{table.Name}");
            sb.AppendLine("  Columns:");
            foreach (var column in table.Columns)
            {
                var pkIndicator = column.IsPrimaryKey ? " (PK)" : "";
                var nullableIndicator = column.IsNullable ? " NULL" : " NOT NULL";
                var maxLength = column.MaxLength > 0 ? $"({column.MaxLength})" : "";
                sb.AppendLine($"    - {column.Name}: {column.DataType}{maxLength}{pkIndicator}{nullableIndicator}");
            }
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

Your task is to verify and fix a SQL query to ensure it is correct and safe.

Original Question: ""{naturalQuery}""

Generated SQL Query (to verify):
{sqlQuery}

Database Schema:
{schemaText}

Instructions:
1. Verify that all table names and column names EXACTLY match those in the schema above,
2. You MUST generate a valid, safe SELECT query that answers the original question, check for any syntax errors or logical issues,
3. Ensure the query ONLY contains SELECT operations (no INSERT, UPDATE, DELETE, DROP, TRUNCATE),
4. Fix any references to tables or columns that do not exist in the schema,
5. Use proper {dbType} syntax,
6. Use table and column names exactly as shown in the schema (case-sensitive),
7. Only use JOINs for tables that exist in the schema,

CRITICAL: If the generated query references tables or columns that don't exist in the schema, you MUST remove those references and rewrite the query using ONLY the tables and columns shown in the schema above.

Return ONLY the corrected SQL query, no explanations or markdown formatting.";

        try
        {
            var verifiedResponse = await _ollamaService.QueryLLMAsync(verificationPrompt);
            var verifiedQuery = CleanupCodeBlocks(verifiedResponse.Trim());
            _logger.LogInformation("Verified and fixed SQL query: {SqlQuery}", verifiedQuery);
            return verifiedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during verification");
            throw;
        }
    }
}


