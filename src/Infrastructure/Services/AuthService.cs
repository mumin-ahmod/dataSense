using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? fullName = null)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "User with this email already exists"
                };
            }

            // Create new user
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false // Require email confirmation in production
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }

            // Assign default "User" role
            await _userManager.AddToRoleAsync(user, "User");

            // Assign default "Free" subscription plan
            var freePlan = await _subscriptionPlanRepository.GetByNameAsync("Free");
            if (freePlan != null)
            {
                await _userSubscriptionRepository.CreateAsync(new Domain.Models.UserSubscription
                {
                    UserId = user.Id,
                    SubscriptionPlanId = freePlan.Id,
                    StartDate = DateTime.UtcNow,
                    IsActive = true,
                    LastResetDate = DateTime.UtcNow
                });
            }

            // Generate tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.Email ?? "", user.UserName, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            // Save refresh token
            await _refreshTokenRepository.CreateAsync(new Domain.Models.RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            _logger.LogInformation("User registered: {Email}", email);

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during registration"
            };
        }
    }

    public async Task<AuthResult> SignInAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password"
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password"
                };
            }

            // Revoke old refresh tokens (optional - can keep multiple devices)
            // await _refreshTokenRepository.RevokeAllForUserAsync(user.Id);

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.Email ?? "", user.UserName, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync();

            // Save refresh token
            await _refreshTokenRepository.CreateAsync(new Domain.Models.RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            _logger.LogInformation("User signed in: {Email}", email);

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sign in");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during sign in"
            };
        }
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid or expired refresh token"
                };
            }

            var user = await _userManager.FindByIdAsync(token.UserId);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            // Revoke old token
            await _refreshTokenRepository.RevokeAsync(refreshToken);

            // Generate new tokens
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user.Id, user.Email ?? "", user.UserName, roles);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync();

            // Save new refresh token
            await _refreshTokenRepository.CreateAsync(new Domain.Models.RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return new AuthResult
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred while refreshing token"
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            return await _refreshTokenRepository.RevokeAsync(refreshToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return false;
        }
    }
}

