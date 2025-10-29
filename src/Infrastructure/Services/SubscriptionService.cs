using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IUsageRequestRepository _usageRepository;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionPlanRepository planRepository,
        IUserSubscriptionRepository subscriptionRepository,
        IUsageRequestRepository usageRepository,
        ILogger<SubscriptionService> logger)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _usageRepository = usageRepository;
        _logger = logger;
    }

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(string planId)
    {
        return await _planRepository.GetByIdAsync(planId);
    }

    public async Task<SubscriptionPlan?> GetPlanByNameAsync(string name)
    {
        return await _planRepository.GetByNameAsync(name);
    }

    public async Task<List<SubscriptionPlan>> GetAllPlansAsync()
    {
        return await _planRepository.GetAllAsync();
    }

    public async Task<UserSubscription?> GetUserSubscriptionAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetByUserIdAsync(userId);
        
        // Reset monthly usage if needed
        if (subscription != null && subscription.LastResetDate.HasValue)
        {
            var lastReset = subscription.LastResetDate.Value;
            var now = DateTime.UtcNow;
            if (lastReset.Year != now.Year || lastReset.Month != now.Month)
            {
                await ResetMonthlyUsageAsync(userId);
                subscription = await _subscriptionRepository.GetByUserIdAsync(userId);
            }
        }

        return subscription;
    }

    public async Task<UserSubscription> AssignPlanToUserAsync(string userId, string planId)
    {
        // Deactivate existing subscription
        var existing = await _subscriptionRepository.GetByUserIdAsync(userId);
        if (existing != null)
        {
            await _subscriptionRepository.DeactivateAsync(existing.Id);
        }

        // Create new subscription
        var subscription = new UserSubscription
        {
            UserId = userId,
            SubscriptionPlanId = planId,
            StartDate = DateTime.UtcNow,
            IsActive = true,
            UsedRequestsThisMonth = 0,
            LastResetDate = DateTime.UtcNow
        };

        return await _subscriptionRepository.CreateAsync(subscription);
    }

    public async Task<bool> CheckRequestLimitAsync(string userId)
    {
        var subscription = await GetUserSubscriptionAsync(userId);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for user: {UserId}", userId);
            return false;
        }

        var plan = await _planRepository.GetByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null)
        {
            _logger.LogWarning("Subscription plan not found: {PlanId}", subscription.SubscriptionPlanId);
            return false;
        }

        // Check if monthly limit is reached
        return subscription.UsedRequestsThisMonth < plan.MonthlyRequestLimit;
    }

    public async Task<bool> IncrementRequestCountAsync(string userId)
    {
        var subscription = await GetUserSubscriptionAsync(userId);
        if (subscription == null)
        {
            return false;
        }

        subscription.UsedRequestsThisMonth++;
        return await _subscriptionRepository.UpdateAsync(subscription);
    }

    public async Task ResetMonthlyUsageAsync(string userId)
    {
        var subscription = await _subscriptionRepository.GetByUserIdAsync(userId);
        if (subscription != null)
        {
            subscription.UsedRequestsThisMonth = 0;
            subscription.LastResetDate = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(subscription);
            _logger.LogInformation("Reset monthly usage for user: {UserId}", userId);
        }
    }
}

