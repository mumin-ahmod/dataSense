using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Contracts;

public class GenerateSqlRequest
{
    public string NaturalQuery { get; set; } = string.Empty;
    public DatabaseSchema Schema { get; set; } = new();
    public string DbType { get; set; } = "sqlserver";
}

public class GenerateSqlResponse
{
    public string SqlQuery { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class InterpretResultsRequest
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string SqlQuery { get; set; } = string.Empty;
    public object Results { get; set; } = new();
}

public class InterpretResultsResponse
{
    public Domain.Models.InterpretationData? Interpretation { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

// Extended Interpretation Request with additional context
public class InterpretResultsRequestExtended : InterpretResultsRequest
{
    public string? AdditionalContext { get; set; } // Can be text or JSON string
}

// Chat Mode Contracts
public class WelcomeSuggestionsRequest
{
    public Domain.Models.DatabaseSchema? Schema { get; set; }
    public string? UserId { get; set; }
}

public class WelcomeSuggestionsResponse
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
}

public class StartConversationRequest
{
    public string? UserId { get; set; }
    public Domain.Models.ConversationType Type { get; set; } = Domain.Models.ConversationType.Regular;
    public string? PlatformType { get; set; } // "whatsapp", "telegram"
    public string? ExternalUserId { get; set; }
    public string? Suggestion { get; set; } // Optional: if user selected a suggestion
}

public class StartConversationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string? Response { get; set; }
    public List<MessageHistoryItem>? MessageHistory { get; set; }
}

public class SendChatMessageRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Domain.Models.DatabaseSchema? Schema { get; set; }
}

public class SendChatMessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public List<MessageHistoryItem> MessageHistory { get; set; } = new();
    public List<Domain.Models.LinkInfo>? Links { get; set; }
    public bool RequiresQueryExecution { get; set; }
    public Domain.Models.InterpretationData? QueryResults { get; set; }
}

public class MessageHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string MessageId { get; set; } = string.Empty;
}

// App Metadata Contracts
public class SaveAppMetadataRequest
{
    public string? AppName { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? ProjectDetails { get; set; }
    public List<Domain.Models.LinkInfo>? Links { get; set; }
    public Domain.Models.DatabaseSchema? Schema { get; set; }
}


