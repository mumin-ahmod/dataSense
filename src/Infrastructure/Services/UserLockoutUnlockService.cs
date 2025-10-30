using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace DataSenseAPI.Infrastructure.Services;

/// <summary>
/// Background service that periodically checks for locked out users and unlocks them
/// after the lockout period has expired
/// </summary>
public class UserLockoutUnlockService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserLockoutUnlockService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    public UserLockoutUnlockService(
        IServiceProvider serviceProvider,
        ILogger<UserLockoutUnlockService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("User Lockout Unlock Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndUnlockUsersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking and unlocking users");
            }

            // Wait for the specified interval before the next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("User Lockout Unlock Service stopped");
    }

    private async Task CheckAndUnlockUsersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        _logger.LogDebug("Checking for locked out users to unlock");

        var allUsers = userManager.Users.ToList();
        var unlockedCount = 0;

        foreach (var user in allUsers)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Check if user is currently locked out
                var isLockedOut = await userManager.IsLockedOutAsync(user);
                if (!isLockedOut)
                    continue;

                // Get lockout end time
                var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                
                // If lockout period has expired, manually unlock the user
                if (lockoutEnd.HasValue && lockoutEnd.Value <= DateTimeOffset.UtcNow)
                {
                    // Set lockout end to past to unlock user
                    var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(-1));
                    
                    if (result.Succeeded)
                    {
                        // Reset failed access count
                        await userManager.ResetAccessFailedCountAsync(user);
                        
                        unlockedCount++;
                        _logger.LogInformation(
                            "User {Email} (ID: {UserId}) unlocked automatically after lockout period expired",
                            user.Email, user.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to unlock user {Email} (ID: {UserId}): {Errors}",
                            user.Email, user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else if (lockoutEnd.HasValue)
                {
                    var timeRemaining = lockoutEnd.Value - DateTimeOffset.UtcNow;
                    _logger.LogDebug(
                        "User {Email} still locked out, {Minutes} minutes remaining",
                        user.Email, timeRemaining.TotalMinutes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user {Email} (ID: {UserId})", user.Email, user.Id);
            }
        }

        if (unlockedCount > 0)
        {
            _logger.LogInformation("Unlocked {Count} user(s) in this cycle", unlockedCount);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("User Lockout Unlock Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}

