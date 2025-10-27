namespace DataSenseAPI.Services;

public interface IQueryExecutor
{
    Task<object> ExecuteQueryAsync(string sqlQuery);
}

