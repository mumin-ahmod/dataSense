using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ChatMessageRepository> _logger;

    public ChatMessageRepository(IDbConnectionFactory connectionFactory, ILogger<ChatMessageRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ChatMessage?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT message_id as Id, conversation_id as ConversationId, role as Role, content as Content, 
                   created_at as Timestamp, metadata::jsonb as Metadata
            FROM messages
            WHERE message_id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<ChatMessageDb>(sql, new { Id = id });
        return result?.ToDomain();
    }

    public async Task<ChatMessage> CreateAsync(ChatMessage message)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO messages (message_id, conversation_id, role, content, created_at, metadata)
            VALUES (@Id, @ConversationId, @Role, @Content, @Timestamp, @Metadata::jsonb)
            RETURNING *";

        var db = ChatMessageDb.FromDomain(message);
        await connection.ExecuteAsync(sql, db);
        return message;
    }

    public async Task<bool> UpdateAsync(ChatMessage message)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE messages
            SET content = @Content,
                metadata = @Metadata::jsonb
            WHERE message_id = @Id";

        var db = ChatMessageDb.FromDomain(message);
        var rowsAffected = await connection.ExecuteAsync(sql, db);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM messages WHERE message_id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<ChatMessage>> GetByConversationIdAsync(string conversationId, int? limit = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT message_id as Id, conversation_id as ConversationId, role as Role, content as Content, 
                   created_at as Timestamp, metadata::jsonb as Metadata
            FROM messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at ASC";

        if (limit.HasValue)
        {
            sql += $" LIMIT {limit.Value}";
        }

        var results = await connection.QueryAsync<ChatMessageDb>(sql, new { ConversationId = conversationId });
        return results.Select(r => r.ToDomain()).ToList();
    }

    public async Task<List<ChatMessage>> GetLatestByConversationIdAsync(string conversationId, int count = 10)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT message_id as Id, conversation_id as ConversationId, role as Role, content as Content, 
                   created_at as Timestamp, metadata::jsonb as Metadata
            FROM messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at DESC
            LIMIT @Count";

        var results = await connection.QueryAsync<ChatMessageDb>(sql, new { ConversationId = conversationId, Count = count });
        return results.Select(r => r.ToDomain()).OrderBy(m => m.Timestamp).ToList();
    }

    private class ChatMessageDb
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Metadata { get; set; }

        public static ChatMessageDb FromDomain(ChatMessage message)
        {
            return new ChatMessageDb
            {
                Id = message.Id,
                ConversationId = message.ConversationId,
                Role = message.Role,
                Content = message.Content,
                Timestamp = message.Timestamp,
                Metadata = message.Metadata != null ? JsonSerializer.Serialize(message.Metadata) : null
            };
        }

        public ChatMessage ToDomain()
        {
            return new ChatMessage
            {
                Id = Id,
                ConversationId = ConversationId,
                Role = Role,
                Content = Content,
                Timestamp = Timestamp,
                Metadata = !string.IsNullOrEmpty(Metadata) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata) 
                    : null
            };
        }
    }
}

