using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.Repositories;

public class UsageRequestRepository : IUsageRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UsageRequestRepository> _logger;

    public UsageRequestRepository(IDbConnectionFactory connectionFactory, ILogger<UsageRequestRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UsageRequest> CreateAsync(UsageRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO usage_requests (request_id, user_id, api_key_id, endpoint, request_type, timestamp, status_code, processing_time_ms, metadata)
            VALUES (@Id::uuid, @UserId, @ApiKeyId::uuid, @Endpoint, @RequestType, @Timestamp, @StatusCode, @ProcessingTimeMs, @Metadata::jsonb)";

        var db = UsageRequestDb.FromDomain(request);
        await connection.ExecuteAsync(sql, db);
        return request;
    }

    public async Task<UsageRequest?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT request_id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata::text as Metadata
            FROM usage_requests
            WHERE request_id = @Id::uuid";

        var result = await connection.QueryFirstOrDefaultAsync<UsageRequestDb>(sql, new { Id = id });
        return result?.ToDomain();
    }

    public async Task<List<UsageRequest>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT request_id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata::text as Metadata
            FROM usage_requests
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

        var results = await connection.QueryAsync<UsageRequestDb>(sql, parameters);
        return results.Select(r => r.ToDomain()).ToList();
    }

    public async Task<List<UsageRequest>> GetByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT request_id as Id, user_id as UserId, api_key_id as ApiKeyId, endpoint as Endpoint, 
                   request_type as RequestType, timestamp as Timestamp, status_code as StatusCode, 
                   processing_time_ms as ProcessingTimeMs, metadata::text as Metadata
            FROM usage_requests
            WHERE api_key_id = @ApiKeyId::uuid";

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

        var results = await connection.QueryAsync<UsageRequestDb>(sql, parameters);
        return results.Select(r => r.ToDomain()).ToList();
    }

    public async Task<int> GetCountByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(*) FROM usage_requests WHERE user_id = @UserId";

        var parameters = new { UserId = userId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND timestamp >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND timestamp <= @ToDate";
        }

        return await connection.QueryFirstOrDefaultAsync<int>(sql, parameters);
    }

    public async Task<int> GetCountByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(*) FROM usage_requests WHERE api_key_id = @ApiKeyId::uuid";

        var parameters = new { ApiKeyId = apiKeyId, FromDate = fromDate, ToDate = toDate };

        if (fromDate.HasValue)
        {
            sql += " AND timestamp >= @FromDate";
        }

        if (toDate.HasValue)
        {
            sql += " AND timestamp <= @ToDate";
        }

        return await connection.QueryFirstOrDefaultAsync<int>(sql, parameters);
    }

    private class UsageRequestDb
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

        public static UsageRequestDb FromDomain(UsageRequest request)
        {
            return new UsageRequestDb
            {
                Id = request.Id,
                UserId = request.UserId,
                ApiKeyId = request.ApiKeyId,
                Endpoint = request.Endpoint,
                RequestType = request.RequestType,
                Timestamp = request.Timestamp,
                StatusCode = request.StatusCode,
                ProcessingTimeMs = request.ProcessingTimeMs,
                Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
            };
        }

        public UsageRequest ToDomain()
        {
            return new UsageRequest
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

