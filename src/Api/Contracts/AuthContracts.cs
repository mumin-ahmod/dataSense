using System.Text.Json.Serialization;

namespace DataSenseAPI.Api.Contracts;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class SignInRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserInfo? User { get; set; }
    
    // Legacy fields for backward compatibility
    public bool Success { get; set; }
    
    // Exclude these fields from serialization when they are null or empty
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AccessToken { get; set; }
    
    public string? UserId { get; set; }
    public string? Email { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> Roles { get; set; } = new();
    
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    
    // Lockout information
    public bool IsLockedOut { get; set; }
    public int? AttemptsRemaining { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string? LockoutTimeRemaining { get; set; }

    // Email confirmation
    public bool EmailConfirmationRequired { get; set; }
    public bool ConfirmationEmailSent { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<MenuPermissionInfo> Permissions { get; set; } = new();
    public string? AgencyId { get; set; }
    public string? EmployeeId { get; set; }
    public UserSubscriptionInfo? Subscription { get; set; }
}

public class UserSubscriptionInfo
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string? PlanDescription { get; set; }
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? AbroadMonthlyPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int UsedRequestsThisMonth { get; set; }
    public DateTime? LastResetDate { get; set; }
}

public class MenuPermissionInfo
{
    public int MenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public List<MenuPermissionInfo> Children { get; set; } = new();
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ResendConfirmationRequest
{
    public string Email { get; set; } = string.Empty;
}

