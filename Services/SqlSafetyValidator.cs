using System.Text.RegularExpressions;

namespace DataSenseAPI.Services;

public class SqlSafetyValidator : ISqlSafetyValidator
{
    private readonly ILogger<SqlSafetyValidator> _logger;
    
    // Dangerous SQL patterns that should be blocked
    private static readonly string[] DangerousKeywords = {
        "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE",
        "EXEC", "EXECUTE", "sp_executesql", "xp_cmdshell"
    };

    public SqlSafetyValidator(ILogger<SqlSafetyValidator> logger)
    {
        _logger = logger;
    }

    public bool IsSafe(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return false;
        }

        var upperQuery = sqlQuery.ToUpperInvariant();
        
        // Must be a SELECT query
        if (!upperQuery.Contains("SELECT"))
        {
            _logger.LogWarning("Query is not a SELECT statement");
            return false;
        }

        // Check for dangerous keywords
        foreach (var keyword in DangerousKeywords)
        {
            // Use word boundary to avoid partial matches
            if (Regex.IsMatch(upperQuery, $@"\b{keyword}\b"))
            {
                _logger.LogWarning($"Query contains dangerous keyword: {keyword}");
                return false;
            }
        }

        return true;
    }

    public string SanitizeQuery(string sqlQuery)
    {
        if (string.IsNullOrWhiteSpace(sqlQuery))
        {
            return sqlQuery;
        }

        // Remove comments
        sqlQuery = Regex.Replace(sqlQuery, @"--.*", "", RegexOptions.Multiline);
        sqlQuery = Regex.Replace(sqlQuery, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Trim whitespace
        return sqlQuery.Trim();
    }
}

