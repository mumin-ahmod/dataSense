namespace DataSenseAPI.Services;

public interface IDatabaseSchemaReader
{
    Task<string> GetSchemaAsync(string? connectionString);
}

