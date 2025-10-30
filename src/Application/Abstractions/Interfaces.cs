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
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey);
    Task<ApiKey?> GetApiKeyByIdAsync(string apiKeyId);
    Task<bool> RevokeApiKeyAsync(string apiKeyId);
}

public sealed class ApiKeyValidationResult
{
    public bool Success { get; init; }
    public string? UserId { get; init; }
    public string? ApiKeyId { get; init; }
}

// Project Service
public interface IProjectService
{
    Task<string> GenerateProjectKeyAsync(string userId, string projectName);
    Task<ProjectValidationResult> ValidateProjectKeyAsync(string projectKey);
    Task<Project?> GetProjectByIdAsync(string projectId);
    Task<List<Project>> GetProjectsByUserAsync(string userId);
}

public sealed class ProjectValidationResult
{
    public bool Success { get; init; }
    public string? UserId { get; init; }
    public string? ProjectId { get; init; }
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

// Authentication Services
public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(string userId, string email, string? userName, IList<string> roles);
    Task<string> GenerateRefreshTokenAsync();
    Task<bool> ValidateRefreshTokenAsync(string token);
}

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null);
    Task<AuthResult> SignInAsync(string email, string password);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
}

public class AuthResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public List<string> Roles { get; init; } = new();
    public string? ErrorMessage { get; init; }
}

// Subscription Service
public interface ISubscriptionService
{
    Task<SubscriptionPlan?> GetPlanByIdAsync(string planId);
    Task<SubscriptionPlan?> GetPlanByNameAsync(string name);
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<UserSubscription?> GetUserSubscriptionAsync(string userId);
    Task<UserSubscription> AssignPlanToUserAsync(string userId, string planId);
    Task<bool> CheckRequestLimitAsync(string userId);
    Task<bool> IncrementRequestCountAsync(string userId);
    Task ResetMonthlyUsageAsync(string userId);
}

