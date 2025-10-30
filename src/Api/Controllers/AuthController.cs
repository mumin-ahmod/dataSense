using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Abstractions;
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
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService, 
        IPermissionService permissionService,
        IRefreshTokenRepository refreshTokenRepository,
        UserManager<IdentityUser> userManager,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _permissionService = permissionService;
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
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

        var result = await _authService.RegisterAsync(request.Email, request.Password, request.FullName);

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
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            UserId = result.UserId,
            Email = result.Email,
            Roles = result.Roles
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
            return Unauthorized(new AuthResponse
            {
                Success = false,
                ErrorMessage = result.ErrorMessage
            });
        }

        // Get menu permissions for user
        var menuPermissions = await _permissionService.GetMenuPermissionsForUserAsync(result.UserId!);
        
        var user = await _userManager.FindByIdAsync(result.UserId!);

        return Ok(new AuthResponse
        {
            Success = true,
            Token = result.AccessToken,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            User = new UserInfo
            {
                Id = result.UserId!,
                Email = result.Email ?? "",
                PhoneNumber = user?.PhoneNumber,
                Roles = result.Roles,
                Permissions = menuPermissions.Select(mp => new MenuPermissionInfo
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
                    CanDelete = mp.CanDelete
                }).ToList()
            },
            UserId = result.UserId,
            Email = result.Email,
            Roles = result.Roles
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

        // Update phone number if provided
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

