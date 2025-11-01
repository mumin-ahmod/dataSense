using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(IDbConnectionFactory connectionFactory, ILogger<PaymentRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Payment?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT payment_id::text as Id, user_id as UserId, subscription_id::text as SubscriptionId,
                   payment_provider as PaymentProvider, transaction_id as TransactionId,
                   amount as Amount, currency as Currency,
                   CASE status 
                       WHEN 'pending' THEN 0
                       WHEN 'completed' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as Status,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM payments
            WHERE payment_id = @Id::uuid";

        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { Id = id });
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT payment_id::text as Id, user_id as UserId, subscription_id::text as SubscriptionId,
                   payment_provider as PaymentProvider, transaction_id as TransactionId,
                   amount as Amount, currency as Currency,
                   CASE status 
                       WHEN 'pending' THEN 0
                       WHEN 'completed' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as Status,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM payments
            WHERE transaction_id = @TransactionId";

        return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { TransactionId = transactionId });
    }

    public async Task<List<Payment>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT payment_id::text as Id, user_id as UserId, subscription_id::text as SubscriptionId,
                   payment_provider as PaymentProvider, transaction_id as TransactionId,
                   amount as Amount, currency as Currency,
                   CASE status 
                       WHEN 'pending' THEN 0
                       WHEN 'completed' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as Status,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM payments
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<Payment>(sql, new { UserId = userId });
        return results.ToList();
    }

    public async Task<List<Payment>> GetBySubscriptionIdAsync(string subscriptionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT payment_id::text as Id, user_id as UserId, subscription_id::text as SubscriptionId,
                   payment_provider as PaymentProvider, transaction_id as TransactionId,
                   amount as Amount, currency as Currency,
                   CASE status 
                       WHEN 'pending' THEN 0
                       WHEN 'completed' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as Status,
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM payments
            WHERE subscription_id = @SubscriptionId::uuid
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<Payment>(sql, new { SubscriptionId = subscriptionId });
        return results.ToList();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO payments (payment_id, user_id, subscription_id, payment_provider, transaction_id, amount, currency, status, created_at, updated_at)
            VALUES (@Id::uuid, @UserId, @SubscriptionId::uuid, @PaymentProvider, @TransactionId, @Amount, @Currency,
                    CASE @Status
                        WHEN 0 THEN 'pending'
                        WHEN 1 THEN 'completed'
                        WHEN 2 THEN 'failed'
                        WHEN 3 THEN 'refunded'
                    END,
                    @CreatedAt, @UpdatedAt)
            RETURNING payment_id::text";

        await connection.ExecuteAsync(sql, new
        {
            Id = payment.Id,
            UserId = payment.UserId,
            SubscriptionId = payment.SubscriptionId,
            PaymentProvider = payment.PaymentProvider,
            TransactionId = payment.TransactionId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = (int)payment.Status,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        });

        return payment;
    }

    public async Task<bool> UpdateAsync(Payment payment)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE payments
            SET payment_provider = @PaymentProvider,
                transaction_id = @TransactionId,
                amount = @Amount,
                currency = @Currency,
                status = CASE @Status
                    WHEN 0 THEN 'pending'
                    WHEN 1 THEN 'completed'
                    WHEN 2 THEN 'failed'
                    WHEN 3 THEN 'refunded'
                END,
                updated_at = @UpdatedAt
            WHERE payment_id = @Id::uuid";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = payment.Id,
            PaymentProvider = payment.PaymentProvider,
            TransactionId = payment.TransactionId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = (int)payment.Status,
            UpdatedAt = payment.UpdatedAt
        });

        return rowsAffected > 0;
    }
}

