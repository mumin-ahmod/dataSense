namespace DataSenseAPI.Services;

public interface ISqlGeneratorService
{
    Task<string> GenerateSqlAsync(string naturalLanguageQuery, string schema);
}

