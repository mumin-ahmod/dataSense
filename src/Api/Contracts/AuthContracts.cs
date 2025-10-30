namespace DataSenseAPI.Api.Contracts;

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FullName { get; set; }
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
    public string? AccessToken { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? ErrorMessage { get; set; }
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
}

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

