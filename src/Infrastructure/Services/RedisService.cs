using System.Text.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisService> _logger;
    private const string ChatHistoryPrefix = "chat:history:";
    private const string ConversationPrefix = "conversation:";
    private const string AppMetadataPrefix = "app:metadata:";

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task SaveChatHistoryAsync(string conversationId, List<ChatMessage> messages)
    {
        try
        {
            var key = $"{ChatHistoryPrefix}{conversationId}";
            var json = JsonSerializer.Serialize(messages);
            await _database.StringSetAsync(key, json, TimeSpan.FromDays(30)); // Keep for 30 days
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chat history to Redis");
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string conversationId)
    {
        try
        {
            var key = $"{ChatHistoryPrefix}{conversationId}";
            var json = await _database.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return new List<ChatMessage>();

            return JsonSerializer.Deserialize<List<ChatMessage>>(json!) ?? new List<ChatMessage>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat history from Redis");
            return new List<ChatMessage>();
        }
    }

    public async Task AddMessageToHistoryAsync(string conversationId, ChatMessage message)
    {
        try
        {
            var history = await GetChatHistoryAsync(conversationId);
            history.Add(message);
            await SaveChatHistoryAsync(conversationId, history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message to chat history");
            throw;
        }
    }

    public async Task SaveConversationAsync(string conversationId, Conversation conversation)
    {
        try
        {
            var key = $"{ConversationPrefix}{conversationId}";
            var json = JsonSerializer.Serialize(conversation);
            await _database.StringSetAsync(key, json, TimeSpan.FromDays(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation to Redis");
            throw;
        }
    }

    public async Task<Conversation?> GetConversationAsync(string conversationId)
    {
        try
        {
            var key = $"{ConversationPrefix}{conversationId}";
            var json = await _database.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<Conversation>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation from Redis");
            return null;
        }
    }

    public async Task SaveAppMetadataAsync(string userId, AppMetadata metadata)
    {
        try
        {
            var key = $"{AppMetadataPrefix}{userId}";
            var json = JsonSerializer.Serialize(metadata);
            await _database.StringSetAsync(key, json, TimeSpan.FromDays(365)); // Keep for 1 year
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving app metadata to Redis");
            throw;
        }
    }

    public async Task<AppMetadata?> GetAppMetadataAsync(string userId)
    {
        try
        {
            var key = $"{AppMetadataPrefix}{userId}";
            var json = await _database.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<AppMetadata>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting app metadata from Redis");
            return null;
        }
    }
}

