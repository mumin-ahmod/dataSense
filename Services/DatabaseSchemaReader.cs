using System.Data;

namespace DataSenseAPI.Services;

public class DatabaseSchemaReader : IDatabaseSchemaReader
{
    private readonly ILogger<DatabaseSchemaReader> _logger;

    public DatabaseSchemaReader(ILogger<DatabaseSchemaReader> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetSchemaAsync(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return await Task.FromResult("No database connection provided");
        }

        try
        {
            // Auto-detect database type from connection string
            if (connectionString.Contains("Server=") && connectionString.Contains("Database="))
            {
                return await GetSqlServerSchemaAsync(connectionString);
            }
            else if (connectionString.Contains("Host=") || connectionString.StartsWith("postgresql://"))
            {
                return await GetPostgreSQLSchemaAsync(connectionString);
            }
            else if (connectionString.Contains("server=") && connectionString.Contains("database=") && connectionString.Contains("uid="))
            {
                return await GetMySqlSchemaAsync(connectionString);
            }
            else if (connectionString.StartsWith("Data Source="))
            {
                return await GetOracleSchemaAsync(connectionString);
            }
            else
            {
                return "Unable to detect database type from connection string";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading database schema");
            return $"Error reading schema: {ex.Message}";
        }
    }

    private async Task<string> GetSqlServerSchemaAsync(string connectionString)
    {
        try
        {
            _logger.LogInformation($"Original connection string: {connectionString.Replace("Password=", "Password=***").Replace("User ID=", "User ID=***")}");
            
            var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            _logger.LogInformation($"Parsed - UserID: '{connectionStringBuilder.UserID}', Password set: {!string.IsNullOrEmpty(connectionStringBuilder.Password)}, Database: '{connectionStringBuilder.InitialCatalog}'");
            
            connectionStringBuilder.ConnectTimeout = 10;
            connectionStringBuilder.IntegratedSecurity = false;
            
            // If UserID is empty, try to extract it from the original connection string
            if (string.IsNullOrEmpty(connectionStringBuilder.UserID) || string.IsNullOrEmpty(connectionStringBuilder.Password))
            {
                _logger.LogWarning("Credentials missing. Attempting manual extraction...");
                
                // Try to manually parse if the builder didn't work
                if (connectionString.Contains("User ID="))
                {
                    var parts = connectionString.Split(';');
                    foreach (var part in parts)
                    {
                        if (part.Trim().StartsWith("User ID="))
                            connectionStringBuilder.UserID = part.Split('=')[1].Trim();
                        if (part.Trim().StartsWith("Password="))
                            connectionStringBuilder.Password = part.Split('=')[1].Trim();
                    }
                }
            }
            
            _logger.LogInformation($"Final - UserID: '{connectionStringBuilder.UserID}', Password set: {!string.IsNullOrEmpty(connectionStringBuilder.Password)}");
            
            if (string.IsNullOrEmpty(connectionStringBuilder.UserID))
            {
                throw new InvalidOperationException("User ID is required for SQL Server authentication");
            }
            
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync();
            
            var schema = new System.Text.StringBuilder();
            var tables = new List<string>();
            
            // Get all tables
            using var tableCommand = new Microsoft.Data.SqlClient.SqlCommand(
                @"SELECT TABLE_SCHEMA, TABLE_NAME 
                  FROM INFORMATION_SCHEMA.TABLES 
                  WHERE TABLE_TYPE = 'BASE TABLE'
                  ORDER BY TABLE_SCHEMA, TABLE_NAME",
                connection);
            
            using var reader = await tableCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schemaName = reader.GetString(0);
                var tableName = reader.GetString(1);
                tables.Add($"{schemaName}.{tableName}");
            }
            
            await reader.CloseAsync();
            
            // Get columns for each table
            foreach (var table in tables)
            {
                var parts = table.Split('.');
                schema.AppendLine($"\nTable: {table}");
                
                using var columnCommand = new Microsoft.Data.SqlClient.SqlCommand(
                    @"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
                      FROM INFORMATION_SCHEMA.COLUMNS
                      WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
                      ORDER BY ORDINAL_POSITION",
                    connection);
                
                columnCommand.Parameters.AddWithValue("@schema", parts[0]);
                columnCommand.Parameters.AddWithValue("@table", parts[1]);
                
                using var columnReader = await columnCommand.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var columnName = columnReader.GetString(0);
                    var dataType = columnReader.GetString(1);
                    var nullable = columnReader.GetString(2);
                    schema.AppendLine($"  - {columnName}: {dataType} {(nullable == "YES" ? "(nullable)" : "")}");
                }
            }
            
            return schema.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading SQL Server schema");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> GetPostgreSQLSchemaAsync(string connectionString)
    {
        try
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            var schema = new System.Text.StringBuilder();
            
            var sql = @"
                SELECT table_schema, table_name 
                FROM information_schema.tables 
                WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                AND table_type = 'BASE TABLE'
                ORDER BY table_schema, table_name";
            
            var tables = new List<(string schema, string name)>();
            
            await using var cmd = new Npgsql.NpgsqlCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add((reader.GetString(0), reader.GetString(1)));
            }
            
            await reader.CloseAsync();
            
            foreach (var (schemaName, tableName) in tables)
            {
                schema.AppendLine($"\nTable: {schemaName}.{tableName}");
                
                var columnSql = @"
                    SELECT column_name, data_type, is_nullable, character_maximum_length
                    FROM information_schema.columns
                    WHERE table_schema = @schema AND table_name = @table
                    ORDER BY ordinal_position";
                
                await using var columnCmd = new Npgsql.NpgsqlCommand(columnSql, connection);
                columnCmd.Parameters.AddWithValue("@schema", schemaName);
                columnCmd.Parameters.AddWithValue("@table", tableName);
                
                await using var columnReader = await columnCmd.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var columnName = columnReader.GetString(0);
                    var dataType = columnReader.GetString(1);
                    var nullable = columnReader.GetString(2);
                    schema.AppendLine($"  - {columnName}: {dataType} {(nullable == "YES" ? "(nullable)" : "")}");
                }
            }
            
            return schema.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading PostgreSQL schema");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> GetMySqlSchemaAsync(string connectionString)
    {
        try
        {
            using var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            var schema = new System.Text.StringBuilder();
            
            using var tableCmd = new MySql.Data.MySqlClient.MySqlCommand(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
                connection);
            
            var tables = new List<string>();
            await using var reader = await tableCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            await reader.CloseAsync();
            
            foreach (var tableName in tables)
            {
                schema.AppendLine($"\nTable: {tableName}");
                
                using var columnCmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @table ORDER BY ORDINAL_POSITION",
                    connection);
                
                columnCmd.Parameters.AddWithValue("@table", tableName);
                
                await using var columnReader = await columnCmd.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var columnName = columnReader.GetString(0);
                    var dataType = columnReader.GetString(1);
                    var nullable = columnReader.GetString(2);
                    schema.AppendLine($"  - {columnName}: {dataType} {(nullable == "YES" ? "(nullable)" : "")}");
                }
            }
            
            return schema.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading MySQL schema");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> GetOracleSchemaAsync(string connectionString)
    {
        try
        {
            using var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString);
            await connection.OpenAsync();
            
            var schema = new System.Text.StringBuilder();
            
            var sql = "SELECT TABLE_NAME FROM USER_TABLES ORDER BY TABLE_NAME";
            var tables = new List<string>();
            
            await using var cmd = new Oracle.ManagedDataAccess.Client.OracleCommand(sql, connection);
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            
            await reader.CloseAsync();
            
            foreach (var tableName in tables)
            {
                schema.AppendLine($"\nTable: {tableName}");
                
                var columnSql = "SELECT COLUMN_NAME, DATA_TYPE, NULLABLE FROM USER_TAB_COLUMNS WHERE TABLE_NAME = :table ORDER BY COLUMN_ID";
                
                await using var columnCmd = new Oracle.ManagedDataAccess.Client.OracleCommand(columnSql, connection);
                columnCmd.Parameters.Add("table", tableName);
                
                await using var columnReader = await columnCmd.ExecuteReaderAsync();
                while (await columnReader.ReadAsync())
                {
                    var columnName = columnReader.GetString(0);
                    var dataType = columnReader.GetString(1);
                    var nullable = columnReader.GetString(2);
                    schema.AppendLine($"  - {columnName}: {dataType} {(nullable == "Y" ? "(nullable)" : "")}");
                }
            }
            
            return schema.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Oracle schema");
            return $"Error: {ex.Message}";
        }
    }
}

