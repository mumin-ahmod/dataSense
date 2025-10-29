using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Services;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("RoleSeeder");

        var roles = new[] { "SystemAdmin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    public static async Task SeedSubscriptionPlansAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var planRepository = scope.ServiceProvider.GetRequiredService<DataSenseAPI.Application.Abstractions.ISubscriptionPlanRepository>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("RoleSeeder");

        // Check if plans already exist
        var existingPlans = await planRepository.GetAllAsync();
        if (existingPlans.Any())
        {
            logger.LogInformation("Subscription plans already exist, skipping seed");
            return;
        }

        // Create default plans
        var freePlan = new DataSenseAPI.Domain.Models.SubscriptionPlan
        {
            Name = "Free",
            Description = "Free tier with basic features",
            MonthlyRequestLimit = 200,
            MonthlyPrice = 0,
            IsActive = true
        };

        var basicPlan = new DataSenseAPI.Domain.Models.SubscriptionPlan
        {
            Name = "Basic",
            Description = "Basic tier with more requests",
            MonthlyRequestLimit = 10000,
            MonthlyPrice = 29.99m,
            IsActive = true
        };

        await planRepository.CreateAsync(freePlan);
        await planRepository.CreateAsync(basicPlan);

        logger.LogInformation("Seeded subscription plans: Free, Basic");
    }
}

