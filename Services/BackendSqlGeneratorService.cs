using System.Text;
using DataSenseAPI.Models;

namespace DataSenseAPI.Services;

/// <summary>
/// Implementation of backend SQL generator that uses provided schema from SDK
/// </summary>
public class BackendSqlGeneratorService : IBackendSqlGeneratorService
{
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<BackendSqlGeneratorService> _logger;

    public BackendSqlGeneratorService(
        IOllamaService ollamaService,
        ILogger<BackendSqlGeneratorService> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<string> GenerateSqlAsync(string naturalQuery, DatabaseSchema schema, string dbType = "sqlserver")
    {
        // Convert schema to text format for LLM
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
            return sqlQuery;
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
}

