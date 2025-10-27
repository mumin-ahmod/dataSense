namespace DataSenseAPI.Services;

public interface ISchemaCacheService
{
    string GetSchema();
    Task RefreshSchemaAsync();
    bool IsSchemaLoaded { get; }
}

