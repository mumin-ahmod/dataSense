namespace DataSenseAPI.Services;

public interface ISqlSafetyValidator
{
    bool IsSafe(string sqlQuery);
    string SanitizeQuery(string sqlQuery);
}

