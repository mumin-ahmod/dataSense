namespace DataSenseAPI.Domain.Models;

public class DatabaseSchema
{
    public string DatabaseName { get; set; } = string.Empty;
    public List<TableInfo> Tables { get; set; } = new();
}

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = "dbo";
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<RelationshipInfo> Relationships { get; set; } = new();
}

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int MaxLength { get; set; }
}

public class RelationshipInfo
{
    public string ForeignKeyTable { get; set; } = string.Empty;
    public string ForeignKeyColumn { get; set; } = string.Empty;
    public string PrimaryKeyTable { get; set; } = string.Empty;
    public string PrimaryKeyColumn { get; set; } = string.Empty;
}

public class InterpretResultsRequest
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public object Results { get; set; } = new();
}

public class InterpretationData
{
    public string Analysis { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

// API Key and User Management
public class ApiKey
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object>? UserMetadata { get; set; }
    public string? SubscriptionPlanId { get; set; } // Reference to subscription plan
}

// Conversations and Chat
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? ApiKeyId { get; set; }
    public string? ProjectId { get; set; }
    public ConversationType Type { get; set; } = ConversationType.Regular;
    public string? PlatformType { get; set; } // "whatsapp", "telegram", etc.
    public string? ExternalUserId { get; set; } // For platform-based chats
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ConversationType
{
    Regular = 0,
    Platform = 1 // WhatsApp, Telegram, etc.
}

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "user", "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Metadata { get; set; }
}

// App Metadata
public class AppMetadata
{
    public string UserId { get; set; } = string.Empty;
    public string? AppName { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? ProjectDetails { get; set; }
    public List<LinkInfo>? Links { get; set; }
    public DatabaseSchema? Schema { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class LinkInfo
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Pricing and Request Tracking
public class RequestLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? ApiKeyId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public RequestType RequestType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int StatusCode { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum RequestType
{
    GenerateSql = 0,
    InterpretResults = 1,
    ChatMessage = 2,
    WelcomeSuggestions = 3
}

public class PricingRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public RequestType RequestType { get; set; }
    public int RequestCount { get; set; }
    public decimal Cost { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
}

// Subscription and Billing
public class SubscriptionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty; // "Free", "Basic", etc.
    public string Description { get; set; } = string.Empty;
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, object>? Features { get; set; }
}

public class UserSubscription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string SubscriptionPlanId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsedRequestsThisMonth { get; set; } = 0;
    public DateTime? LastResetDate { get; set; }
}

public class UsageRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? ApiKeyId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public RequestType RequestType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int StatusCode { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
}


// Projects and Project Keys
public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string MessageChannel { get; set; } = "telegram"; // aligns with message_channels names
    public string? ChannelNumber { get; set; } // e.g., phone/handle for the channel
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string ProjectKeyHash { get; set; } = string.Empty; // hashed classification key
}

// Menus and Role-based permissions
public class Menu
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class RolePermission
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public int MenuId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}


