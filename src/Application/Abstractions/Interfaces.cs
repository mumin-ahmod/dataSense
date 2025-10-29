using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Abstractions;

public interface IBackendSqlGeneratorService
{
    Task<string> GenerateSqlAsync(string naturalQuery, DatabaseSchema schema, string dbType = "sqlserver");
}

public interface IBackendResultInterpreterService
{
    Task<InterpretationData> InterpretResultsAsync(InterpretResultsRequest request);
}

public interface ISqlSafetyValidator
{
    bool IsSafe(string sqlQuery);
    string SanitizeQuery(string sqlQuery);
}

public interface IOllamaService
{
    Task<string> QueryLLMAsync(string prompt);
}

// Redis Service for Chat History and Caching
public interface IRedisService
{
    Task SaveChatHistoryAsync(string conversationId, List<ChatMessage> messages);
    Task<List<ChatMessage>> GetChatHistoryAsync(string conversationId);
    Task AddMessageToHistoryAsync(string conversationId, ChatMessage message);
    Task SaveConversationAsync(string conversationId, Conversation conversation);
    Task<Conversation?> GetConversationAsync(string conversationId);
    Task SaveAppMetadataAsync(string userId, AppMetadata metadata);
    Task<AppMetadata?> GetAppMetadataAsync(string userId);
}

// Kafka Service for Request Queuing
public interface IKafkaService
{
    Task ProduceAsync(string topic, string message, Dictionary<string, string>? headers = null);
    Task ProduceRequestLogAsync(RequestLog log);
    Task ProducePricingRecordAsync(PricingRecord record);
    Task ProduceOllamaRequestAsync(string conversationId, string prompt, Dictionary<string, object>? metadata = null);
}

// API Key Service
public interface IApiKeyService
{
    Task<string> GenerateApiKeyAsync(string userId, string name, Dictionary<string, object>? metadata = null);
    Task<bool> ValidateApiKeyAsync(string apiKey, out string? userId, out string? apiKeyId);
    Task<ApiKey?> GetApiKeyByIdAsync(string apiKeyId);
    Task<bool> RevokeApiKeyAsync(string apiKeyId);
}

// Conversation Service
public interface IConversationService
{
    Task<Conversation> CreateConversationAsync(string userId, string? apiKeyId = null, ConversationType type = ConversationType.Regular, string? platformType = null, string? externalUserId = null);
    Task<Conversation?> GetConversationByIdAsync(string conversationId);
    Task<List<Conversation>> GetUserConversationsAsync(string userId);
}

// LLM Query Detection Service
public interface IQueryDetectionService
{
    Task<bool> NeedsQueryExecutionAsync(string message, DatabaseSchema? schema);
}

// App Metadata Service
public interface IAppMetadataService
{
    Task SaveAppMetadataAsync(string userId, AppMetadata metadata);
    Task<AppMetadata?> GetAppMetadataAsync(string userId);
    Task<List<string>> GenerateWelcomeSuggestionsAsync(DatabaseSchema? schema);
}


