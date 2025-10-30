using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Authentication controller for user registration and sign-in
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
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
    /// Sign in with email and password
    /// </summary>
    [HttpPost("signin")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> SignIn([FromBody] SignInRequest request)
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
}

