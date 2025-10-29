using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.Repositories;

public class RequestLogRepository : IRequestLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<RequestLogRepository> _logger;

    public RequestLogRepository(IDbConnectionFactory connectionFactory, ILogger<RequestLogRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<RequestLog?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata as Metadata
            FROM request_logs
            WHERE id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<RequestLogDb>(sql, new { Id = id });
        return result?.ToDomain();
    }

    public async Task<RequestLog> CreateAsync(RequestLog log)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO request_logs (id, user_id, api_key_id, endpoint, request_type, timestamp, status_code, processing_time_ms, metadata)
            VALUES (@Id, @UserId, @ApiKeyId, @Endpoint, @RequestType, @Timestamp, @StatusCode, @ProcessingTimeMs, @Metadata::jsonb)
            RETURNING *";

        var db = RequestLogDb.FromDomain(log);
        await connection.ExecuteAsync(sql, db);
        return log;
    }

    public async Task<bool> UpdateAsync(RequestLog log)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE request_logs
            SET status_code = @StatusCode,
                processing_time_ms = @ProcessingTimeMs,
                metadata = @Metadata::jsonb
            WHERE id = @Id";

        var db = RequestLogDb.FromDomain(log);
        var rowsAffected = await connection.ExecuteAsync(sql, db);
        return rowsAffected > 0;
    }

    public async Task<List<RequestLog>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata as Metadata
            FROM request_logs
            WHERE user_id = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate, Limit = limit };

        if (fromDate.HasValue)
        {
            sql += " AND timestamp >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND timestamp <= @ToDate";
        }

        sql += " ORDER BY timestamp DESC";

        if (limit.HasValue)
        {
            sql += $" LIMIT {limit.Value}";
        }

        var results = await connection.QueryAsync<RequestLogDb>(sql, parameters);
        return results.Select(r => r.ToDomain()).ToList();
    }

    public async Task<List<RequestLog>> GetByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata as Metadata
            FROM request_logs
            WHERE api_key_id = @ApiKeyId";

        var parameters = new { ApiKeyId = apiKeyId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND timestamp >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND timestamp <= @ToDate";
        }

        sql += " ORDER BY timestamp DESC";

        var results = await connection.QueryAsync<RequestLogDb>(sql, parameters);
        return results.Select(r => r.ToDomain()).ToList();
    }

    private class RequestLogDb
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? ApiKeyId { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public RequestType RequestType { get; set; }
        public DateTime Timestamp { get; set; }
        public int StatusCode { get; set; }
        public long? ProcessingTimeMs { get; set; }
        public string? Metadata { get; set; }

        public static RequestLogDb FromDomain(RequestLog log)
        {
            return new RequestLogDb
            {
                Id = log.Id,
                UserId = log.UserId,
                ApiKeyId = log.ApiKeyId,
                Endpoint = log.Endpoint,
                RequestType = log.RequestType,
                Timestamp = log.Timestamp,
                StatusCode = log.StatusCode,
                ProcessingTimeMs = log.ProcessingTimeMs,
                Metadata = log.Metadata != null ? JsonSerializer.Serialize(log.Metadata) : null
            };
        }

        public RequestLog ToDomain()
        {
            return new RequestLog
            {
                Id = Id,
                UserId = UserId,
                ApiKeyId = ApiKeyId,
                Endpoint = Endpoint,
                RequestType = RequestType,
                Timestamp = Timestamp,
                StatusCode = StatusCode,
                ProcessingTimeMs = ProcessingTimeMs,
                Metadata = !string.IsNullOrEmpty(Metadata) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata) 
                    : null
            };
        }
    }
}

