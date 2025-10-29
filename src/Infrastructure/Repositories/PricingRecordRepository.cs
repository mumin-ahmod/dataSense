using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class PricingRecordRepository : IPricingRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PricingRecordRepository> _logger;

    public PricingRecordRepository(IDbConnectionFactory connectionFactory, ILogger<PricingRecordRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<PricingRecord?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, RequestType, RequestCount, Cost, Date
            FROM PricingRecords
            WHERE Id = @Id";

        return await connection.QueryFirstOrDefaultAsync<PricingRecord>(sql, new { Id = id });
    }

    public async Task<PricingRecord> CreateAsync(PricingRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO PricingRecords (Id, UserId, RequestType, RequestCount, Cost, Date)
            VALUES (@Id, @UserId, @RequestType, @RequestCount, @Cost, @Date)
            ON CONFLICT (UserId, Date, RequestType) 
            DO UPDATE SET RequestCount = PricingRecords.RequestCount + @RequestCount,
                         Cost = PricingRecords.Cost + @Cost
            RETURNING *";

        await connection.ExecuteAsync(sql, record);
        return record;
    }

    public async Task<bool> UpdateAsync(PricingRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE PricingRecords
            SET RequestCount = @RequestCount,
                Cost = @Cost
            WHERE Id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, record);
        return rowsAffected > 0;
    }

    public async Task<PricingRecord?> GetByUserIdAndDateAsync(string userId, DateTime date, RequestType? requestType = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT Id, UserId, RequestType, RequestCount, Cost, Date
            FROM PricingRecords
            WHERE UserId = @UserId AND Date = @Date";

        var parameters = new { UserId = userId, Date = date.Date };

        if (requestType.HasValue)
        {
            sql += " AND RequestType = @RequestType";
            parameters = new { UserId = userId, Date = date.Date, RequestType = requestType.Value };
        }

        return await connection.QueryFirstOrDefaultAsync<PricingRecord>(sql, parameters);
    }

    public async Task<List<PricingRecord>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT Id, UserId, RequestType, RequestCount, Cost, Date
            FROM PricingRecords
            WHERE UserId = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND Date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND Date <= @ToDate";
        }

        sql += " ORDER BY Date DESC";

        var results = await connection.QueryAsync<PricingRecord>(sql, parameters);
        return results.ToList();
    }

    public async Task<decimal> GetTotalCostByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COALESCE(SUM(Cost), 0) FROM PricingRecords WHERE UserId = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND Date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND Date <= @ToDate";
        }

        var result = await connection.QueryFirstOrDefaultAsync<decimal?>(sql, parameters);
        return result ?? 0;
    }
}

