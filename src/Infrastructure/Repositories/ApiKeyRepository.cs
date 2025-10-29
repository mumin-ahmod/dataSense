using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ApiKeyRepository> _logger;

    public ApiKeyRepository(IDbConnectionFactory connectionFactory, ILogger<ApiKeyRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ApiKey?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, KeyHash, Name, IsActive, CreatedAt, ExpiresAt, 
                   UserMetadata::jsonb as UserMetadata
            FROM ApiKeys
            WHERE Id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<ApiKeyDb>(sql, new { Id = id });
        return result?.ToDomain();
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, KeyHash, Name, IsActive, CreatedAt, ExpiresAt, 
                   UserMetadata::jsonb as UserMetadata
            FROM ApiKeys
            WHERE KeyHash = @KeyHash";

        var result = await connection.QueryFirstOrDefaultAsync<ApiKeyDb>(sql, new { KeyHash = keyHash });
        return result?.ToDomain();
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO ApiKeys (Id, UserId, KeyHash, Name, IsActive, CreatedAt, ExpiresAt, UserMetadata)
            VALUES (@Id, @UserId, @KeyHash, @Name, @IsActive, @CreatedAt, @ExpiresAt, @UserMetadata::jsonb)
            RETURNING *";

        var db = ApiKeyDb.FromDomain(apiKey);
        await connection.ExecuteAsync(sql, db);
        return apiKey;
    }

    public async Task<bool> UpdateAsync(ApiKey apiKey)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE ApiKeys
            SET IsActive = @IsActive,
                ExpiresAt = @ExpiresAt,
                UserMetadata = @UserMetadata::jsonb
            WHERE Id = @Id";

        var db = ApiKeyDb.FromDomain(apiKey);
        var rowsAffected = await connection.ExecuteAsync(sql, db);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM ApiKeys WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<ApiKey>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, KeyHash, Name, IsActive, CreatedAt, ExpiresAt, 
                   UserMetadata::jsonb as UserMetadata
            FROM ApiKeys
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC";

        var results = await connection.QueryAsync<ApiKeyDb>(sql, new { UserId = userId });
        return results.Select(r => r.ToDomain()).ToList();
    }

    // Helper class for Dapper mapping
    private class ApiKeyDb
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? UserMetadata { get; set; }

        public static ApiKeyDb FromDomain(ApiKey apiKey)
        {
            return new ApiKeyDb
            {
                Id = apiKey.Id,
                UserId = apiKey.UserId,
                KeyHash = apiKey.KeyHash,
                Name = apiKey.Name,
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt,
                UserMetadata = apiKey.UserMetadata != null ? JsonSerializer.Serialize(apiKey.UserMetadata) : null
            };
        }

        public ApiKey ToDomain()
        {
            return new ApiKey
            {
                Id = Id,
                UserId = UserId,
                KeyHash = KeyHash,
                Name = Name,
                IsActive = IsActive,
                CreatedAt = CreatedAt,
                ExpiresAt = ExpiresAt,
                UserMetadata = !string.IsNullOrEmpty(UserMetadata) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(UserMetadata) 
                    : null
            };
        }
    }
}

