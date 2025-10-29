using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ConversationRepository> _logger;

    public ConversationRepository(IDbConnectionFactory connectionFactory, ILogger<ConversationRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Conversation?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, ApiKeyId, Type, PlatformType, ExternalUserId, 
                   CreatedAt, UpdatedAt, IsActive
            FROM Conversations
            WHERE Id = @Id AND IsActive = true";

        return await connection.QueryFirstOrDefaultAsync<Conversation>(sql, new { Id = id });
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO Conversations (Id, UserId, ApiKeyId, Type, PlatformType, ExternalUserId, CreatedAt, UpdatedAt, IsActive)
            VALUES (@Id, @UserId, @ApiKeyId, @Type, @PlatformType, @ExternalUserId, @CreatedAt, @UpdatedAt, @IsActive)
            RETURNING *";

        await connection.ExecuteAsync(sql, conversation);
        return conversation;
    }

    public async Task<bool> UpdateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE Conversations
            SET UpdatedAt = @UpdatedAt,
                IsActive = @IsActive
            WHERE Id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, conversation);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE Conversations SET IsActive = false WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, ApiKeyId, Type, PlatformType, ExternalUserId, 
                   CreatedAt, UpdatedAt, IsActive
            FROM Conversations
            WHERE UserId = @UserId AND IsActive = true
            ORDER BY COALESCE(UpdatedAt, CreatedAt) DESC";

        var results = await connection.QueryAsync<Conversation>(sql, new { UserId = userId });
        return results.ToList();
    }

    public async Task<List<Conversation>> GetByExternalUserIdAsync(string externalUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT Id, UserId, ApiKeyId, Type, PlatformType, ExternalUserId, 
                   CreatedAt, UpdatedAt, IsActive
            FROM Conversations
            WHERE ExternalUserId = @ExternalUserId AND IsActive = true
            ORDER BY COALESCE(UpdatedAt, CreatedAt) DESC";

        var results = await connection.QueryAsync<Conversation>(sql, new { ExternalUserId = externalUserId });
        return results.ToList();
    }
}

