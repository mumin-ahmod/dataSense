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
            SELECT conversation_id::text as Id, user_id as UserId, api_key_id::text as ApiKeyId, 
                   project_id::text as ProjectId, type as Type, 
                   platform_type as PlatformType, external_user_id as ExternalUserId, 
                   created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive
            FROM conversations
            WHERE conversation_id = @Id::uuid AND is_active = true";

        return await connection.QueryFirstOrDefaultAsync<Conversation>(sql, new { Id = id });
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO conversations (conversation_id, user_id, api_key_id, project_id, type, platform_type, external_user_id, created_at, updated_at, is_active)
            VALUES (@Id::uuid, @UserId, @ApiKeyId::uuid, @ProjectId::uuid, @Type, @PlatformType, @ExternalUserId, @CreatedAt, @UpdatedAt, @IsActive)
            RETURNING conversation_id::text";

        await connection.ExecuteAsync(sql, conversation);
        return conversation;
    }

    public async Task<bool> UpdateAsync(Conversation conversation)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE conversations
            SET updated_at = @UpdatedAt,
                is_active = @IsActive
            WHERE conversation_id = @Id::uuid";

        var rowsAffected = await connection.ExecuteAsync(sql, conversation);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE conversations SET is_active = false WHERE conversation_id = @Id::uuid";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT conversation_id::text as Id, user_id as UserId, api_key_id::text as ApiKeyId, 
                   project_id::text as ProjectId, type as Type, 
                   platform_type as PlatformType, external_user_id as ExternalUserId, 
                   created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive
            FROM conversations
            WHERE user_id = @UserId AND is_active = true
            ORDER BY COALESCE(updated_at, created_at) DESC";

        var results = await connection.QueryAsync<Conversation>(sql, new { UserId = userId });
        return results.ToList();
    }

    public async Task<List<Conversation>> GetByExternalUserIdAsync(string externalUserId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT conversation_id::text as Id, user_id as UserId, api_key_id::text as ApiKeyId, 
                   project_id::text as ProjectId, type as Type, 
                   platform_type as PlatformType, external_user_id as ExternalUserId, 
                   created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive
            FROM conversations
            WHERE external_user_id = @ExternalUserId AND is_active = true
            ORDER BY COALESCE(updated_at, created_at) DESC";

        var results = await connection.QueryAsync<Conversation>(sql, new { ExternalUserId = externalUserId });
        return results.ToList();
    }
}

