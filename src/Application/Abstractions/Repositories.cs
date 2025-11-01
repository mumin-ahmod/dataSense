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

// Subscription Repositories
public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(string id);
    Task<SubscriptionPlan?> GetByNameAsync(string name);
    Task<List<SubscriptionPlan>> GetAllAsync();
    Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan);
    Task<bool> UpdateAsync(SubscriptionPlan plan);
    Task<bool> DeleteAsync(string id);
}

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByIdAsync(string id);
    Task<UserSubscription?> GetByUserIdAsync(string userId);
    Task<UserSubscription> CreateAsync(UserSubscription subscription);
    Task<bool> UpdateAsync(UserSubscription subscription);
    Task<bool> DeactivateAsync(string id);
}

public interface IUsageRequestRepository
{
    Task<UsageRequest> CreateAsync(UsageRequest request);
    Task<UsageRequest?> GetByIdAsync(string id);
    Task<List<UsageRequest>> GetByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null);
    Task<List<UsageRequest>> GetByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<int> GetCountByUserIdAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<int> GetCountByApiKeyIdAsync(string apiKeyId, DateTime? fromDate = null, DateTime? toDate = null);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetByUserIdAsync(string userId);
    Task<RefreshToken> CreateAsync(RefreshToken token);
    Task<bool> RevokeAsync(string token);
    Task<bool> RevokeAllForUserAsync(string userId);
    Task<bool> DeleteExpiredTokensAsync();
}

public interface IMenuRepository
{
    Task<Menu?> GetByIdAsync(int id);
    Task<List<Menu>> GetAllAsync();
    Task<List<Menu>> GetActiveMenusAsync();
    Task<Menu> CreateAsync(Menu menu);
    Task<bool> UpdateAsync(Menu menu);
    Task<bool> DeleteAsync(int id);
    Task<List<Menu>> GetByParentIdAsync(int? parentId);
}

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByIdAsync(int id);
    Task<List<RolePermission>> GetAllAsync();
    Task<List<RolePermission>> GetByRoleIdAsync(string roleId);
    Task<List<RolePermission>> GetByMenuIdAsync(int menuId);
    Task<RolePermission?> GetByRoleAndMenuAsync(string roleId, int menuId);
    Task<RolePermission> CreateAsync(RolePermission permission);
    Task<bool> UpdateAsync(RolePermission permission);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByRoleIdAsync(string roleId);
    Task<bool> DeleteByMenuIdAsync(int menuId);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(string id);
    Task<Project?> GetByKeyHashAsync(string keyHash);
    Task<List<Project>> GetByUserIdAsync(string userId);
    Task<List<Project>> GetAllAsync();
    Task<Project> CreateAsync(Project project);
    Task<bool> UpdateAsync(Project project);
    Task<bool> ToggleActiveAsync(string id);
    Task<bool> DeleteAsync(string id);
    Task<string> GetProjectKeyByProjectIdAsync(string projectId);
    Task<bool> UpdateProjectKeyAsync(string projectId, string keyHash);
}

