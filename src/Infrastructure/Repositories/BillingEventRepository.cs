using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class BillingEventRepository : IBillingEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<BillingEventRepository> _logger;

    public BillingEventRepository(IDbConnectionFactory connectionFactory, ILogger<BillingEventRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<BillingEvent?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT billing_event_id::text as Id, subscription_id::text as SubscriptionId,
                   api_key_id::text as ApiKeyId, request_id::text as RequestId,
                   tokens_used as TokensUsed, estimated_cost as EstimatedCost,
                   CASE event_type
                       WHEN 'request' THEN 0
                       WHEN 'overage' THEN 1
                       WHEN 'subscription' THEN 2
                       WHEN 'refund' THEN 3
                       WHEN 'cart' THEN 4
                       WHEN 'initiated' THEN 5
                   END as EventType,
                   idempotency_key as IdempotencyKey,
                   created_at as CreatedAt
            FROM billing_events
            WHERE billing_event_id = @Id::uuid";

        return await connection.QueryFirstOrDefaultAsync<BillingEvent>(sql, new { Id = id });
    }

    public async Task<BillingEvent?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT billing_event_id::text as Id, subscription_id::text as SubscriptionId,
                   api_key_id::text as ApiKeyId, request_id::text as RequestId,
                   tokens_used as TokensUsed, estimated_cost as EstimatedCost,
                   CASE event_type
                       WHEN 'request' THEN 0
                       WHEN 'overage' THEN 1
                       WHEN 'subscription' THEN 2
                       WHEN 'refund' THEN 3
                       WHEN 'cart' THEN 4
                       WHEN 'initiated' THEN 5
                   END as EventType,
                   idempotency_key as IdempotencyKey,
                   created_at as CreatedAt
            FROM billing_events
            WHERE idempotency_key = @IdempotencyKey";

        return await connection.QueryFirstOrDefaultAsync<BillingEvent>(sql, new { IdempotencyKey = idempotencyKey });
    }

    public async Task<List<BillingEvent>> GetBySubscriptionIdAsync(string subscriptionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT billing_event_id::text as Id, subscription_id::text as SubscriptionId,
                   api_key_id::text as ApiKeyId, request_id::text as RequestId,
                   tokens_used as TokensUsed, estimated_cost as EstimatedCost,
                   CASE event_type
                       WHEN 'request' THEN 0
                       WHEN 'overage' THEN 1
                       WHEN 'subscription' THEN 2
                       WHEN 'refund' THEN 3
                       WHEN 'cart' THEN 4
                       WHEN 'initiated' THEN 5
                   END as EventType,
                   idempotency_key as IdempotencyKey,
                   created_at as CreatedAt
            FROM billing_events
            WHERE subscription_id = @SubscriptionId::uuid
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<BillingEvent>(sql, new { SubscriptionId = subscriptionId });
        return results.ToList();
    }

    public async Task<BillingEvent> CreateAsync(BillingEvent billingEvent)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO billing_events (billing_event_id, subscription_id, api_key_id, request_id, tokens_used, estimated_cost, event_type, idempotency_key, created_at)
            VALUES (@Id::uuid, @SubscriptionId::uuid, @ApiKeyId::uuid, @RequestId::uuid, @TokensUsed, @EstimatedCost,
                    CASE @EventType
                        WHEN 0 THEN 'request'
                        WHEN 1 THEN 'overage'
                        WHEN 2 THEN 'subscription'
                        WHEN 3 THEN 'refund'
                        WHEN 4 THEN 'cart'
                        WHEN 5 THEN 'initiated'
                    END,
                    @IdempotencyKey, @CreatedAt)
            RETURNING billing_event_id::text";

        await connection.ExecuteAsync(sql, new
        {
            Id = billingEvent.Id,
            SubscriptionId = billingEvent.SubscriptionId,
            ApiKeyId = billingEvent.ApiKeyId,
            RequestId = billingEvent.RequestId,
            TokensUsed = billingEvent.TokensUsed,
            EstimatedCost = billingEvent.EstimatedCost,
            EventType = (int)billingEvent.EventType,
            IdempotencyKey = billingEvent.IdempotencyKey,
            CreatedAt = billingEvent.CreatedAt
        });

        return billingEvent;
    }
}

