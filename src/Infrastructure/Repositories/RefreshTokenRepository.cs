using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory, ILogger<RefreshTokenRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, user_id as UserId, token as Token, expires_at as ExpiresAt, 
                   created_at as CreatedAt, is_revoked as IsRevoked, replaced_by_token as ReplacedByToken
            FROM refresh_tokens
            WHERE token = @Token";

        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { Token = token });
    }

    public async Task<RefreshToken?> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, user_id as UserId, token as Token, expires_at as ExpiresAt, 
                   created_at as CreatedAt, is_revoked as IsRevoked, replaced_by_token as ReplacedByToken
            FROM refresh_tokens
            WHERE user_id = @UserId AND is_revoked = false AND expires_at > @Now
            ORDER BY created_at DESC
            LIMIT 1";

        return await connection.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { UserId = userId, Now = DateTime.UtcNow });
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken token)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO refresh_tokens (id, user_id, token, expires_at, created_at, is_revoked, replaced_by_token)
            VALUES (@Id, @UserId, @Token, @ExpiresAt, @CreatedAt, @IsRevoked, @ReplacedByToken)
            RETURNING *";

        await connection.ExecuteAsync(sql, token);
        return token;
    }

    public async Task<bool> RevokeAsync(string token)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE refresh_tokens SET is_revoked = true WHERE token = @Token";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Token = token });
        return rowsAffected > 0;
    }

    public async Task<bool> RevokeAllForUserAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE refresh_tokens SET is_revoked = true WHERE user_id = @UserId AND is_revoked = false";
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteExpiredTokensAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM refresh_tokens WHERE expires_at < @Now";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Now = DateTime.UtcNow });
        return rowsAffected > 0;
    }
}

