using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class UsageRecordRepository : IUsageRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UsageRecordRepository> _logger;

    public UsageRecordRepository(IDbConnectionFactory connectionFactory, ILogger<UsageRecordRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UsageRecord?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, user_id as UserId, api_key_id::text as ApiKeyId,
                   request_type as RequestType, request_count as RequestCount,
                   request_left as RequestLeft, date as Date
            FROM usage_records
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<UsageRecord>(sql, new { Id = id });
    }

    public async Task<UsageRecord?> GetByUserIdAndDateAsync(string userId, DateTime date, RequestType? requestType = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, api_key_id::text as ApiKeyId,
                   request_type as RequestType, request_count as RequestCount,
                   request_left as RequestLeft, date as Date
            FROM usage_records
            WHERE user_id = @UserId AND date = @Date";

        if (requestType.HasValue)
        {
            sql += " AND request_type = @RequestType";
        }

        sql += " LIMIT 1";

        return await connection.QueryFirstOrDefaultAsync<UsageRecord>(sql, new 
        { 
            UserId = userId, 
            Date = date.Date,
            RequestType = requestType.HasValue ? (int)requestType.Value : (int?)null
        });
    }

    public async Task<List<UsageRecord>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, api_key_id::text as ApiKeyId,
                   request_type as RequestType, request_count as RequestCount,
                   request_left as RequestLeft, date as Date
            FROM usage_records
            WHERE user_id = @UserId";

        if (fromDate.HasValue)
        {
            sql += " AND date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND date <= @ToDate";
        }

        sql += " ORDER BY date DESC";

        var results = await connection.QueryAsync<UsageRecord>(sql, new 
        { 
            UserId = userId,
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date
        });

        return results.ToList();
    }

    public async Task<List<UsageRecord>> GetByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, api_key_id::text as ApiKeyId,
                   request_type as RequestType, request_count as RequestCount,
                   request_left as RequestLeft, date as Date
            FROM usage_records
            WHERE api_key_id = @ApiKeyId::uuid";

        if (fromDate.HasValue)
        {
            sql += " AND date >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND date <= @ToDate";
        }

        sql += " ORDER BY date DESC";

        var results = await connection.QueryAsync<UsageRecord>(sql, new 
        { 
            ApiKeyId = apiKeyId,
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date
        });

        return results.ToList();
    }

    public async Task<UsageRecord> CreateAsync(UsageRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO usage_records (id, user_id, api_key_id, request_type, request_count, request_left, date)
            VALUES (@Id, @UserId, @ApiKeyId::uuid, @RequestType, @RequestCount, @RequestLeft, @Date)
            ON CONFLICT (user_id, date, request_type) 
            DO UPDATE SET
                request_count = usage_records.request_count + @RequestCount,
                request_left = @RequestLeft
            RETURNING id";

        await connection.ExecuteAsync(sql, new
        {
            Id = record.Id,
            UserId = record.UserId,
            ApiKeyId = string.IsNullOrEmpty(record.ApiKeyId) ? (Guid?)null : Guid.Parse(record.ApiKeyId),
            RequestType = (int)record.RequestType,
            RequestCount = record.RequestCount,
            RequestLeft = record.RequestLeft,
            Date = record.Date.Date
        });

        return record;
    }

    public async Task<bool> UpdateAsync(UsageRecord record)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE usage_records
            SET request_count = @RequestCount,
                request_left = @RequestLeft,
                api_key_id = @ApiKeyId::uuid
            WHERE id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = record.Id,
            RequestCount = record.RequestCount,
            RequestLeft = record.RequestLeft,
            ApiKeyId = string.IsNullOrEmpty(record.ApiKeyId) ? (Guid?)null : Guid.Parse(record.ApiKeyId)
        });

        return rowsAffected > 0;
    }
}

