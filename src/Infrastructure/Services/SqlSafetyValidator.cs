using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;

namespace DataSenseAPI.Infrastructure.Services;

public class SqlSafetyValidator : ISqlSafetyValidator
{
    private readonly ILogger<SqlSafetyValidator> _logger;

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
        if (string.IsNullOrWhiteSpace(sqlQuery)) return false;
        var upperQuery = sqlQuery.ToUpperInvariant();
        if (!upperQuery.Contains("SELECT"))
        {
            _logger.LogWarning("Query is not a SELECT statement");
            return false;
        }
        foreach (var keyword in DangerousKeywords)
        {
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
        if (string.IsNullOrWhiteSpace(sqlQuery)) return sqlQuery;
        sqlQuery = Regex.Replace(sqlQuery, @"--.*", "", RegexOptions.Multiline);
        sqlQuery = Regex.Replace(sqlQuery, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return sqlQuery.Trim();
    }
}


