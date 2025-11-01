using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UserSubscriptionRepository> _logger;

    public UserSubscriptionRepository(IDbConnectionFactory connectionFactory, ILogger<UserSubscriptionRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UserSubscription?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT subscription_id::text as Id, user_id as UserId, plan_id::text as SubscriptionPlanId, 
                   start_date as StartDate, end_date as EndDate, 
                   CASE WHEN status = 'active' THEN true ELSE false END as IsActive,
                   requests_used as UsedRequestsThisMonth, last_reset_date as LastResetDate
            FROM user_subscriptions
            WHERE subscription_id = @Id::uuid";

        return await connection.QueryFirstOrDefaultAsync<UserSubscription>(sql, new { Id = id });
    }

    public async Task<UserSubscription?> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        // Trim userId to handle any whitespace issues
        userId = userId?.Trim() ?? string.Empty;
        
        const string sql = @"
            SELECT subscription_id::text as Id, user_id as UserId, plan_id::text as SubscriptionPlanId, 
                   start_date as StartDate, end_date as EndDate, 
                   CASE WHEN status = 'active' THEN true ELSE false END as IsActive,
                   requests_used as UsedRequestsThisMonth, last_reset_date as LastResetDate
            FROM user_subscriptions
            WHERE user_id = @UserId
            ORDER BY 
                CASE WHEN status = 'active' THEN 0 ELSE 1 END,
                start_date DESC
            LIMIT 1";

        var result = await connection.QueryFirstOrDefaultAsync<UserSubscription>(sql, new { UserId = userId });
        
        // If exact match fails, try case-insensitive
        if (result == null)
        {
            const string caseInsensitiveSql = @"
                SELECT subscription_id::text as Id, user_id as UserId, plan_id::text as SubscriptionPlanId, 
                       start_date as StartDate, end_date as EndDate, 
                       CASE WHEN status = 'active' THEN true ELSE false END as IsActive,
                       requests_used as UsedRequestsThisMonth, last_reset_date as LastResetDate
                FROM user_subscriptions
                WHERE LOWER(user_id) = LOWER(@UserId)
                ORDER BY 
                    CASE WHEN status = 'active' THEN 0 ELSE 1 END,
                    start_date DESC
                LIMIT 1";
            
            result = await connection.QueryFirstOrDefaultAsync<UserSubscription>(caseInsensitiveSql, new { UserId = userId });
        }
        
        return result;
    }

    public async Task<UserSubscription> CreateAsync(UserSubscription subscription)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO user_subscriptions (subscription_id, user_id, plan_id, start_date, end_date, status, requests_used, last_reset_date)
            VALUES (@Id::uuid, @UserId, @SubscriptionPlanId::uuid, @StartDate, @EndDate, 
                    CASE WHEN @IsActive THEN 'active' ELSE 'canceled' END, 
                    @UsedRequestsThisMonth, @LastResetDate)
            RETURNING subscription_id::text";

        await connection.ExecuteAsync(sql, subscription);
        return subscription;
    }

    public async Task<bool> UpdateAsync(UserSubscription subscription)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE user_subscriptions
            SET plan_id = @SubscriptionPlanId::uuid,
                end_date = @EndDate,
                status = CASE WHEN @IsActive THEN 'active' ELSE 'canceled' END,
                requests_used = @UsedRequestsThisMonth,
                last_reset_date = @LastResetDate
            WHERE subscription_id = @Id::uuid";

        var rowsAffected = await connection.ExecuteAsync(sql, subscription);
        return rowsAffected > 0;
    }

    public async Task<bool> DeactivateAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE user_subscriptions SET status = 'canceled' WHERE subscription_id = @Id::uuid";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}

