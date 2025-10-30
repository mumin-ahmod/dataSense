using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System;
using System.Linq;
using System.Text;

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
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ILogger<AuthService> logger,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _logger = logger;
        _emailSender = emailSender;
        _configuration = configuration;
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

            var roles = await _userManager.GetRolesAsync(user);

            // Generate email confirmation token and send email
            var confirmationSent = await SendEmailConfirmationAsync(user);

            _logger.LogInformation("User registered: {Email}", email);

            return new AuthResult
            {
                Success = true,
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList(),
                EmailConfirmationRequired = true,
                ConfirmationEmailSent = confirmationSent,
                Message = confirmationSent
                    ? "Confirmation email sent. Please check your inbox to verify your account."
                    : "Registration successful, but we could not send the confirmation email. Please contact support."
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

            if (!user.EmailConfirmed)
            {
                var confirmationSent = await SendEmailConfirmationAsync(user);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Email not confirmed. Please check your inbox for the confirmation link.",
                    EmailConfirmationRequired = true,
                    ConfirmationEmailSent = confirmationSent,
                    Message = confirmationSent
                        ? "Email not confirmed. We've re-sent the confirmation email to your inbox."
                        : "Email not confirmed and we could not re-send the confirmation email. Please contact support."
                };
            }

            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var timeRemaining = lockoutEnd.HasValue 
                    ? lockoutEnd.Value.UtcDateTime - DateTime.UtcNow 
                    : TimeSpan.Zero;

                _logger.LogWarning("Login attempt for locked out user: {Email}, Lockout ends at: {LockoutEnd}", 
                    email, lockoutEnd);

                return new AuthResult
                {
                    Success = false,
                    IsLockedOut = true,
                    LockoutEnd = lockoutEnd?.UtcDateTime,
                    LockoutDuration = timeRemaining,
                    ErrorMessage = $"Account is locked due to multiple failed login attempts. " +
                                 $"Please try again after {timeRemaining.TotalMinutes:F0} minutes."
                };
            }

            // Attempt sign in with lockout on failure
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            
            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var timeRemaining = lockoutEnd.HasValue 
                    ? lockoutEnd.Value.UtcDateTime - DateTime.UtcNow 
                    : TimeSpan.FromHours(1);

                _logger.LogWarning("User locked out after failed login: {Email}, Lockout ends at: {LockoutEnd}", 
                    email, lockoutEnd);

                return new AuthResult
                {
                    Success = false,
                    IsLockedOut = true,
                    LockoutEnd = lockoutEnd?.UtcDateTime,
                    LockoutDuration = timeRemaining,
                    AttemptsRemaining = 0,
                    ErrorMessage = $"Account has been locked due to multiple failed login attempts. " +
                                 $"Please try again in {timeRemaining.TotalMinutes:F0} minutes or contact support."
                };
            }

            if (!result.Succeeded)
            {
                // Get failed access count
                var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
                var maxAttempts = _userManager.Options.Lockout.MaxFailedAccessAttempts;
                var attemptsRemaining = maxAttempts - failedAttempts;

                _logger.LogWarning("Failed login attempt for user: {Email}, Attempts: {FailedAttempts}/{MaxAttempts}", 
                    email, failedAttempts, maxAttempts);

                return new AuthResult
                {
                    Success = false,
                    AttemptsRemaining = attemptsRemaining,
                    ErrorMessage = attemptsRemaining > 0 
                        ? $"Invalid email or password. You have {attemptsRemaining} attempt(s) remaining before your account is locked."
                        : "Invalid email or password"
                };
            }

            // Successful login - reset failed access count
            await _userManager.ResetAccessFailedCountAsync(user);

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

            _logger.LogInformation("User signed in successfully: {Email}", email);

            return new AuthResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList(),
                IsLockedOut = false,
                AttemptsRemaining = null
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

    public async Task<AuthResult> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            if (user.EmailConfirmed)
            {
                return new AuthResult
                {
                    Success = true,
                    UserId = user.Id,
                    Email = user.Email,
                    Message = "Email is already confirmed."
                };
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description)),
                    EmailConfirmationRequired = true
                };
            }

            _logger.LogInformation("Email confirmed for user {UserId}", user.Id);

            return new AuthResult
            {
                Success = true,
                UserId = user.Id,
                Email = user.Email,
                Message = "Email confirmed successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred while confirming email"
            };
        }
    }

    private async Task<bool> SendEmailConfirmationAsync(IdentityUser user)
    {
        if (user.Email == null)
        {
            return false;
        }

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationLink = BuildEmailConfirmationLink(user.Id, encodedToken);

            var subject = "Confirm your DataSense account";
            var body = $"<p>Hello,</p><p>Thank you for registering with DataSense.</p>" +
                       $"<p>Please confirm your email by clicking the link below:</p>" +
                       $"<p><a href=\"{confirmationLink}\" style=\"display:inline-block;padding:12px 20px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:4px;\">Confirm Email</a></p>" +
                       $"<p>If the button does not work, copy and paste this link into your browser:<br/><span style=\"word-break:break-all;\">{confirmationLink}</span></p>" +
                       "<p>If you did not create this account, you can ignore this email.</p>" +
                       "<p>— DataSense Team</p>";

            await _emailSender.QueueEmailAsync(user.Email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
            return false;
        }
    }

    public async Task<AuthResult> ResendConfirmationEmailAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "User not found"
                };
            }

            if (user.EmailConfirmed)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Email is already confirmed"
                };
            }

            var confirmationSent = await SendEmailConfirmationAsync(user);

            if (!confirmationSent)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Failed to send confirmation email",
                    EmailConfirmationRequired = true
                };
            }

            _logger.LogInformation("Confirmation email resent to {Email}", email);

            return new AuthResult
            {
                Success = true,
                UserId = user.Id,
                Email = user.Email,
                ConfirmationEmailSent = true,
                Message = "Confirmation email has been resent. Please check your inbox."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending confirmation email to {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred while resending confirmation email"
            };
        }
    }

    private string BuildEmailConfirmationLink(string userId, string encodedToken)
    {
        var explicitUrl = _configuration["App:EmailConfirmationUrl"];
        string endpoint;

        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            endpoint = explicitUrl.TrimEnd('/');
        }
        else
        {
            var configuredUrls = _configuration["Urls"];
            var baseAddress = (configuredUrls?.Split(';', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "http://localhost:5050").TrimEnd('/');
            endpoint = $"{baseAddress}/api/v1/auth/confirm-email";
        }

        return $"{endpoint}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(encodedToken)}";
    }
}

