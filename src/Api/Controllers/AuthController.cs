using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Authentication controller for user registration and sign-in
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService, 
        IPermissionService permissionService,
        IRefreshTokenRepository refreshTokenRepository,
        UserManager<ApplicationUser> userManager,
        IUserSubscriptionRepository userSubscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _permissionService = permissionService;
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _userSubscriptionRepository = userSubscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = "Email and password are required"
            });
        }

        var result = await _authService.RegisterAsync(request.Email, request.Password, request.FirstName, request.LastName);

        if (!result.Success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            UserId = result.UserId,
            Email = result.Email,
            Roles = result.Roles,
            EmailConfirmationRequired = result.EmailConfirmationRequired,
            ConfirmationEmailSent = result.ConfirmationEmailSent,
            Message = result.Message ?? "Registration successful. Please confirm your email to activate your account."
        });
    }

    /// <summary>
    /// Confirm a user's email address
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = "UserId and token are required",
                EmailConfirmationRequired = true
            });
        }

        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (!result.Success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                EmailConfirmationRequired = result.EmailConfirmationRequired,
                Message = result.Message
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            UserId = result.UserId,
            Email = result.Email,
            Message = result.Message ?? "Email confirmed successfully. You can now log in."
        });
    }

    /// <summary>
    /// Resend confirmation email for a user who hasn't confirmed their email yet
    /// </summary>
    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = "Email is required"
            });
        }

        var result = await _authService.ResendConfirmationEmailAsync(request.Email);

        if (!result.Success)
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                EmailConfirmationRequired = result.EmailConfirmationRequired,
                Message = result.Message
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Message = result.Message ?? "Confirmation email has been resent.",
            ConfirmationEmailSent = result.ConfirmationEmailSent
        });
    }

    /// <summary>
    /// Sign in with email and password (alias for login)
    /// </summary>
    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
    {
        return await Login(request);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] SignInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = "Email and password are required"
            });
        }

        var result = await _authService.SignInAsync(request.Email, request.Password);

        if (!result.Success)
        {
            var response = new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                IsLockedOut = result.IsLockedOut,
                AttemptsRemaining = result.AttemptsRemaining,
                LockoutEnd = result.LockoutEnd,
                EmailConfirmationRequired = result.EmailConfirmationRequired,
                ConfirmationEmailSent = result.ConfirmationEmailSent,
                Message = result.Message
            };

            // Add human-readable lockout time remaining
            if (result.LockoutEnd.HasValue)
            {
                var timeRemaining = result.LockoutEnd.Value - DateTime.UtcNow;
                if (timeRemaining.TotalMinutes > 0)
                {
                    response.LockoutTimeRemaining = $"{timeRemaining.TotalMinutes:F0} minutes";
                }
            }

            // Return 423 Locked for locked out accounts, 401 Unauthorized for invalid credentials
            if (result.EmailConfirmationRequired && !result.IsLockedOut)
            {
                return StatusCode(403, response);
            }

            return result.IsLockedOut ? StatusCode(423, response) : Unauthorized(response);
        }

        // Get menu permissions for user
        var menuPermissions = await _permissionService.GetMenuPermissionsForUserAsync(result.UserId!);
        
        var user = await _userManager.FindByIdAsync(result.UserId!);

        // Convert to MenuPermissionInfo and build hierarchical structure
        var allPermissions = menuPermissions.Select(mp => new MenuPermissionInfo
        {
            MenuId = mp.MenuId,
            MenuName = mp.MenuName,
            DisplayName = mp.DisplayName,
            Icon = mp.Icon,
            Url = mp.Url,
            ParentId = mp.ParentId,
            Order = mp.Order,
            CanView = mp.CanView,
            CanCreate = mp.CanCreate,
            CanEdit = mp.CanEdit,
            CanDelete = mp.CanDelete,
            Children = new List<MenuPermissionInfo>()
        }).ToList();

        // Build hierarchical structure
        var permissionDict = allPermissions.ToDictionary(p => p.MenuId);
        var hierarchicalPermissions = new List<MenuPermissionInfo>();

        foreach (var permission in allPermissions)
        {
            if (permission.ParentId.HasValue)
            {
                // This is a child menu - add it to its parent's Children list
                if (permissionDict.TryGetValue(permission.ParentId.Value, out var parent))
                {
                    parent.Children.Add(permission);
                }
            }
            else
            {
                // This is a top-level menu
                hierarchicalPermissions.Add(permission);
            }
        }

        // Sort by Order and sort children within each parent
        hierarchicalPermissions = hierarchicalPermissions
            .OrderBy(p => p.Order)
            .ThenBy(p => p.MenuId)
            .ToList();

        foreach (var permission in hierarchicalPermissions)
        {
            permission.Children = permission.Children
                .OrderBy(c => c.Order)
                .ThenBy(c => c.MenuId)
                .ToList();
        }

        // Get user's subscription
        UserSubscriptionInfo? subscriptionInfo = null;
        var userSubscription = await _userSubscriptionRepository.GetByUserIdAsync(result.UserId!);
        if (userSubscription != null)
        {
            var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(userSubscription.SubscriptionPlanId);
            if (subscriptionPlan != null)
            {
                subscriptionInfo = new UserSubscriptionInfo
                {
                    SubscriptionId = userSubscription.Id,
                    PlanId = subscriptionPlan.Id,
                    PlanName = subscriptionPlan.Name,
                    PlanDescription = subscriptionPlan.Description,
                    MonthlyRequestLimit = subscriptionPlan.MonthlyRequestLimit,
                    MonthlyPrice = subscriptionPlan.MonthlyPrice,
                    AbroadMonthlyPrice = subscriptionPlan.AbroadMonthlyPrice,
                    StartDate = userSubscription.StartDate,
                    EndDate = userSubscription.EndDate,
                    IsActive = userSubscription.IsActive,
                    UsedRequestsThisMonth = userSubscription.UsedRequestsThisMonth,
                    LastResetDate = userSubscription.LastResetDate
                };
            }
            else
            {
                // Subscription exists but plan not found
                subscriptionInfo = new UserSubscriptionInfo
                {
                    SubscriptionId = userSubscription.Id,
                    PlanId = userSubscription.SubscriptionPlanId,
                    PlanName = "Unknown Plan",
                    StartDate = userSubscription.StartDate,
                    EndDate = userSubscription.EndDate,
                    IsActive = userSubscription.IsActive,
                    UsedRequestsThisMonth = userSubscription.UsedRequestsThisMonth,
                    LastResetDate = userSubscription.LastResetDate
                };
            }
        }

        return Ok(new AuthResponse
        {
            Success = true,
            Token = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            User = new UserInfo
            {
                Id = result.UserId!,
                Email = result.Email ?? "",
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                PhoneNumber = user?.PhoneNumber,
                Roles = result.Roles,
                Permissions = hierarchicalPermissions,
                Subscription = subscriptionInfo
            },
            UserId = result.UserId,
            Email = result.Email,
            Message = result.Message
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new AuthResponse
            {
                Success = false,
                ErrorMessage = "Refresh token is required"
            });
        }

        var result = await _authService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage
            });
        }

        return Ok(new AuthResponse
        {
            Success = true,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            UserId = result.UserId,
            Email = result.Email,
            Roles = result.Roles
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("UserId")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get menu permissions for user
        var menuPermissions = await _permissionService.GetMenuPermissionsForUserAsync(userId);

        // Convert to MenuPermissionInfo and build hierarchical structure
        var allPermissions = menuPermissions.Select(mp => new MenuPermissionInfo
        {
            MenuId = mp.MenuId,
            MenuName = mp.MenuName,
            DisplayName = mp.DisplayName,
            Icon = mp.Icon,
            Url = mp.Url,
            ParentId = mp.ParentId,
            Order = mp.Order,
            CanView = mp.CanView,
            CanCreate = mp.CanCreate,
            CanEdit = mp.CanEdit,
            CanDelete = mp.CanDelete,
            Children = new List<MenuPermissionInfo>()
        }).ToList();

        // Build hierarchical structure
        var permissionDict = allPermissions.ToDictionary(p => p.MenuId);
        var hierarchicalPermissions = new List<MenuPermissionInfo>();

        foreach (var permission in allPermissions)
        {
            if (permission.ParentId.HasValue)
            {
                // This is a child menu - add it to its parent's Children list
                if (permissionDict.TryGetValue(permission.ParentId.Value, out var parent))
                {
                    parent.Children.Add(permission);
                }
            }
            else
            {
                // This is a top-level menu
                hierarchicalPermissions.Add(permission);
            }
        }

        // Sort by Order and sort children within each parent
        hierarchicalPermissions = hierarchicalPermissions
            .OrderBy(p => p.Order)
            .ThenBy(p => p.MenuId)
            .ToList();

        foreach (var permission in hierarchicalPermissions)
        {
            permission.Children = permission.Children
                .OrderBy(c => c.Order)
                .ThenBy(c => c.MenuId)
                .ToList();
        }

        // Get user's subscription
        UserSubscriptionInfo? subscriptionInfo = null;
        try
        {
            var userSubscription = await _userSubscriptionRepository.GetByUserIdAsync(userId);
            if (userSubscription != null)
            {
                var subscriptionPlan = await _subscriptionPlanRepository.GetByIdAsync(userSubscription.SubscriptionPlanId);
                if (subscriptionPlan != null)
                {
                    subscriptionInfo = new UserSubscriptionInfo
                    {
                        SubscriptionId = userSubscription.Id,
                        PlanId = subscriptionPlan.Id,
                        PlanName = subscriptionPlan.Name,
                        PlanDescription = subscriptionPlan.Description,
                        MonthlyRequestLimit = subscriptionPlan.MonthlyRequestLimit,
                        MonthlyPrice = subscriptionPlan.MonthlyPrice,
                        AbroadMonthlyPrice = subscriptionPlan.AbroadMonthlyPrice,
                        StartDate = userSubscription.StartDate,
                        EndDate = userSubscription.EndDate,
                        IsActive = userSubscription.IsActive,
                        UsedRequestsThisMonth = userSubscription.UsedRequestsThisMonth,
                        LastResetDate = userSubscription.LastResetDate
                    };
                }
                else
                {
                    _logger.LogWarning("Subscription plan not found for SubscriptionId={SubscriptionId}, PlanId={PlanId}", 
                        userSubscription.Id, userSubscription.SubscriptionPlanId);

                    // Subscription exists but plan not found
                    subscriptionInfo = new UserSubscriptionInfo
                    {
                        SubscriptionId = userSubscription.Id,
                        PlanId = userSubscription.SubscriptionPlanId,
                        PlanName = "Unknown Plan",
                        StartDate = userSubscription.StartDate,
                        EndDate = userSubscription.EndDate,
                        IsActive = userSubscription.IsActive,
                        UsedRequestsThisMonth = userSubscription.UsedRequestsThisMonth,
                        LastResetDate = userSubscription.LastResetDate
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription for user {UserId}", userId);
            // Don't fail the entire request, just log the error and continue without subscription info
        }

        var userInfo = new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Roles = roles.ToList(),
            Permissions = hierarchicalPermissions,
            Subscription = subscriptionInfo
        };

        return Ok(userInfo);
    }

    /// <summary>
    /// Revoke refresh token (sign out)
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        
        if (!result)
        {
            return BadRequest(new { error = "Failed to revoke token" });
        }

        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Update profile fields if provided
        if (request.FirstName != null)
        {
            user.FirstName = request.FirstName;
        }
        
        if (request.LastName != null)
        {
            user.LastName = request.LastName;
        }
        
        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = request.PhoneNumber;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = "Failed to update profile", details = result.Errors });
        }

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(new 
        { 
            message = "Profile updated successfully",
            user = new 
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber
            }
        });
    }

    /// <summary>
    /// Logout from all devices by revoking all refresh tokens
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "User not authenticated" });
        }

        var result = await _refreshTokenRepository.RevokeAllForUserAsync(userId);
        
        if (!result)
        {
            return BadRequest(new { error = "Failed to logout from all devices" });
        }

        _logger.LogInformation("User {UserId} logged out from all devices", userId);

        return Ok(new { message = "Logged out from all devices successfully" });
    }
}

