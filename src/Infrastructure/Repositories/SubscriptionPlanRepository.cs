using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataSenseAPI.Infrastructure.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SubscriptionPlanRepository> _logger;

    public SubscriptionPlanRepository(IDbConnectionFactory connectionFactory, ILogger<SubscriptionPlanRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT plan_id as Id, name as Name, description as Description, 
                   request_limit_per_month as MonthlyRequestLimit, monthly_price as MonthlyPrice, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt, 
                   features::jsonb as Features
            FROM subscription_plans
            WHERE plan_id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<SubscriptionPlanDb>(sql, new { Id = id });
        return result?.ToDomain();
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT plan_id as Id, name as Name, description as Description, 
                   request_limit_per_month as MonthlyRequestLimit, monthly_price as MonthlyPrice, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt, 
                   features::jsonb as Features
            FROM subscription_plans
            WHERE name = @Name AND is_active = true";

        var result = await connection.QueryFirstOrDefaultAsync<SubscriptionPlanDb>(sql, new { Name = name });
        return result?.ToDomain();
    }

    public async Task<List<SubscriptionPlan>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT plan_id as Id, name as Name, description as Description, 
                   request_limit_per_month as MonthlyRequestLimit, monthly_price as MonthlyPrice, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt, 
                   features::jsonb as Features
            FROM subscription_plans
            WHERE is_active = true
            ORDER BY monthly_price ASC";

        var results = await connection.QueryAsync<SubscriptionPlanDb>(sql);
        return results.Select(r => r.ToDomain()).ToList();
    }

    public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO subscription_plans (plan_id, name, description, request_limit_per_month, monthly_price, is_active, created_at, updated_at, features)
            VALUES (@Id, @Name, @Description, @MonthlyRequestLimit, @MonthlyPrice, @IsActive, @CreatedAt, @UpdatedAt, @Features::jsonb)
            RETURNING *";

        var db = SubscriptionPlanDb.FromDomain(plan);
        await connection.ExecuteAsync(sql, db);
        return plan;
    }

    public async Task<bool> UpdateAsync(SubscriptionPlan plan)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE subscription_plans
            SET name = @Name,
                description = @Description,
                request_limit_per_month = @MonthlyRequestLimit,
                monthly_price = @MonthlyPrice,
                is_active = @IsActive,
                updated_at = @UpdatedAt,
                features = @Features::jsonb
            WHERE plan_id = @Id";

        var db = SubscriptionPlanDb.FromDomain(plan);
        var rowsAffected = await connection.ExecuteAsync(sql, db);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "UPDATE subscription_plans SET is_active = false WHERE plan_id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    private class SubscriptionPlanDb
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int MonthlyRequestLimit { get; set; }
        public decimal MonthlyPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Features { get; set; }

        public static SubscriptionPlanDb FromDomain(SubscriptionPlan plan)
        {
            return new SubscriptionPlanDb
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyRequestLimit = plan.MonthlyRequestLimit,
                MonthlyPrice = plan.MonthlyPrice,
                IsActive = plan.IsActive,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                Features = plan.Features != null ? JsonSerializer.Serialize(plan.Features) : null
            };
        }

        public SubscriptionPlan ToDomain()
        {
            return new SubscriptionPlan
            {
                Id = Id,
                Name = Name,
                Description = Description,
                MonthlyRequestLimit = MonthlyRequestLimit,
                MonthlyPrice = MonthlyPrice,
                IsActive = IsActive,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Features = !string.IsNullOrEmpty(Features) 
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(Features) 
                    : null
            };
        }
    }
}

