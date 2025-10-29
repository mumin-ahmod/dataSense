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
            SELECT id as Id, user_id as UserId, request_type as RequestType, 
                   request_count as RequestCount, cost as Cost, date as Date
            FROM pricing_records
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<PricingRecord>(sql, new { Id = id });
    }

    public async Task<PricingRecord> CreateAsync(PricingRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO pricing_records (id, user_id, request_type, request_count, cost, date)
            VALUES (@Id, @UserId, @RequestType, @RequestCount, @Cost, @Date)
            ON CONFLICT (user_id, date, request_type) 
            DO UPDATE SET request_count = pricing_records.request_count + @RequestCount,
                         cost = pricing_records.cost + @Cost
            RETURNING *";

        await connection.ExecuteAsync(sql, record);
        return record;
    }

    public async Task<bool> UpdateAsync(PricingRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE pricing_records
            SET request_count = @RequestCount,
                cost = @Cost
            WHERE id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, record);
        return rowsAffected > 0;
    }

    public async Task<PricingRecord?> GetByUserIdAndDateAsync(string userId, DateTime date, RequestType? requestType = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, request_type as RequestType, 
                   request_count as RequestCount, cost as Cost, date as Date
            FROM pricing_records
            WHERE user_id = @UserId AND date = @Date";

        if (requestType.HasValue)
        {
            sql += " AND request_type = @RequestType";
            return await connection.QueryFirstOrDefaultAsync<PricingRecord>(sql, new { UserId = userId, Date = date.Date, RequestType = requestType.Value });
        }

        return await connection.QueryFirstOrDefaultAsync<PricingRecord>(sql, new { UserId = userId, Date = date.Date });
    }

    public async Task<List<PricingRecord>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, request_type as RequestType, 
                   request_count as RequestCount, cost as Cost, date as Date
            FROM pricing_records
            WHERE user_id = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND date <= @ToDate";
        }

        sql += " ORDER BY date DESC";

        var results = await connection.QueryAsync<PricingRecord>(sql, parameters);
        return results.ToList();
    }

    public async Task<decimal> GetTotalCostByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COALESCE(SUM(cost), 0) FROM pricing_records WHERE user_id = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND date <= @ToDate";
        }

        var result = await connection.QueryFirstOrDefaultAsync<decimal?>(sql, parameters);
        return result ?? 0;
    }
}

