using System.Data;

namespace DataSenseAPI.Services;

public class QueryExecutor : IQueryExecutor
{
    private readonly ISchemaCacheService _schemaCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueryExecutor> _logger;
    private const int MaxRows = 1000;

    public QueryExecutor(
        ISchemaCacheService schemaCache,
        IConfiguration configuration,
        ILogger<QueryExecutor> logger)
    {
        _schemaCache = schemaCache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<object> ExecuteQueryAsync(string sqlQuery)
    {
        try
        {
            var connectionString = GetConnectionString();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("No database connection string configured");
            }

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new Microsoft.Data.SqlClient.SqlCommand(sqlQuery, connection);
            command.CommandTimeout = 30;

            using var reader = await command.ExecuteReaderAsync();
            
            var result = new List<Dictionary<string, object?>>();
            var columnNames = new List<string>();

            // Get column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            int rowCount = 0;
            while (await reader.ReadAsync() && rowCount < MaxRows)
            {
                var row = new Dictionary<string, object?>();
                
                foreach (var columnName in columnNames)
                {
                    var value = reader[columnName];
                    row[columnName] = value == DBNull.Value ? null : value;
                }
                
                result.Add(row);
                rowCount++;
            }

            if (rowCount >= MaxRows)
            {
                _logger.LogWarning($"Query results limited to {MaxRows} rows");
            }

            return new
            {
                rows = result,
                rowCount = result.Count,
                columns = columnNames
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL query");
            throw;
        }
    }

    private string? GetConnectionString()
    {
        var envConnection = Environment.GetEnvironmentVariable("DBCONNECTION");
        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            return envConnection;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _configuration.GetConnectionString("ConnectionString");
        }

        return connectionString;
    }
}

