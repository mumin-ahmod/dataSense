using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Abstractions;

// Repository interfaces for Dapper-based data access
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(string id);
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<bool> UpdateAsync(ApiKey apiKey);
    Task<bool> DeleteAsync(string id);
    Task<List<ApiKey>> GetByUserIdAsync(string userId);
}

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string id);
    Task<Conversation> CreateAsync(Conversation conversation);
    Task<bool> UpdateAsync(Conversation conversation);
    Task<bool> DeleteAsync(string id);
    Task<List<Conversation>> GetByUserIdAsync(string userId);
    Task<List<Conversation>> GetByExternalUserIdAsync(string externalUserId);
}

public interface IChatMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(string id);
    Task<ChatMessage> CreateAsync(ChatMessage message);
    Task<bool> UpdateAsync(ChatMessage message);
    Task<bool> DeleteAsync(string id);
    Task<List<ChatMessage>> GetByConversationIdAsync(string conversationId, int? limit = null);
    Task<List<ChatMessage>> GetLatestByConversationIdAsync(string conversationId, int count = 10);
}

public interface IRequestLogRepository
{
    Task<RequestLog?> GetByIdAsync(string id);
    Task<RequestLog> CreateAsync(RequestLog log);
    Task<bool> UpdateAsync(RequestLog log);
    Task<List<RequestLog>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null);
    Task<List<RequestLog>> GetByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IPricingRecordRepository
{
    Task<PricingRecord?> GetByIdAsync(string id);
    Task<PricingRecord> CreateAsync(PricingRecord record);
    Task<bool> UpdateAsync(PricingRecord record);
    Task<PricingRecord?> GetByUserIdAndDateAsync(string userId, DateTime date, RequestType? requestType = null);
    Task<List<PricingRecord>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> GetTotalCostByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
}

