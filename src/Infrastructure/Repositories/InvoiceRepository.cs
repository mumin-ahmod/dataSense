using System.Data;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<InvoiceRepository> _logger;

    public InvoiceRepository(IDbConnectionFactory connectionFactory, ILogger<InvoiceRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Invoice?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT invoice_id::text as Id, subscription_id::text as SubscriptionId, user_id as UserId,
                   invoice_number as InvoiceNumber, period_start as PeriodStart, period_end as PeriodEnd,
                   total_amount as TotalAmount, currency as Currency,
                   CASE payment_status 
                       WHEN 'unpaid' THEN 0
                       WHEN 'paid' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as PaymentStatus,
                   created_at as CreatedAt
            FROM invoices
            WHERE invoice_id = @Id::uuid";

        return await connection.QueryFirstOrDefaultAsync<Invoice>(sql, new { Id = id });
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT invoice_id::text as Id, subscription_id::text as SubscriptionId, user_id as UserId,
                   invoice_number as InvoiceNumber, period_start as PeriodStart, period_end as PeriodEnd,
                   total_amount as TotalAmount, currency as Currency,
                   CASE payment_status 
                       WHEN 'unpaid' THEN 0
                       WHEN 'paid' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as PaymentStatus,
                   created_at as CreatedAt
            FROM invoices
            WHERE invoice_number = @InvoiceNumber";

        return await connection.QueryFirstOrDefaultAsync<Invoice>(sql, new { InvoiceNumber = invoiceNumber });
    }

    public async Task<List<Invoice>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT invoice_id::text as Id, subscription_id::text as SubscriptionId, user_id as UserId,
                   invoice_number as InvoiceNumber, period_start as PeriodStart, period_end as PeriodEnd,
                   total_amount as TotalAmount, currency as Currency,
                   CASE payment_status 
                       WHEN 'unpaid' THEN 0
                       WHEN 'paid' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as PaymentStatus,
                   created_at as CreatedAt
            FROM invoices
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<Invoice>(sql, new { UserId = userId });
        return results.ToList();
    }

    public async Task<List<Invoice>> GetBySubscriptionIdAsync(string subscriptionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT invoice_id::text as Id, subscription_id::text as SubscriptionId, user_id as UserId,
                   invoice_number as InvoiceNumber, period_start as PeriodStart, period_end as PeriodEnd,
                   total_amount as TotalAmount, currency as Currency,
                   CASE payment_status 
                       WHEN 'unpaid' THEN 0
                       WHEN 'paid' THEN 1
                       WHEN 'failed' THEN 2
                       WHEN 'refunded' THEN 3
                   END as PaymentStatus,
                   created_at as CreatedAt
            FROM invoices
            WHERE subscription_id = @SubscriptionId::uuid
            ORDER BY created_at DESC";

        var results = await connection.QueryAsync<Invoice>(sql, new { SubscriptionId = subscriptionId });
        return results.ToList();
    }

    public async Task<Invoice> CreateAsync(Invoice invoice)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO invoices (invoice_id, subscription_id, user_id, invoice_number, period_start, period_end, total_amount, currency, payment_status, created_at)
            VALUES (@Id::uuid, @SubscriptionId::uuid, @UserId, @InvoiceNumber, @PeriodStart, @PeriodEnd, @TotalAmount, @Currency,
                    CASE @PaymentStatus
                        WHEN 0 THEN 'unpaid'
                        WHEN 1 THEN 'paid'
                        WHEN 2 THEN 'failed'
                        WHEN 3 THEN 'refunded'
                    END,
                    @CreatedAt)
            RETURNING invoice_id::text";

        await connection.ExecuteAsync(sql, new
        {
            Id = invoice.Id,
            SubscriptionId = invoice.SubscriptionId,
            UserId = invoice.UserId,
            InvoiceNumber = invoice.InvoiceNumber,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            PaymentStatus = (int)invoice.PaymentStatus,
            CreatedAt = invoice.CreatedAt
        });

        return invoice;
    }

    public async Task<bool> UpdateAsync(Invoice invoice)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE invoices
            SET period_start = @PeriodStart,
                period_end = @PeriodEnd,
                total_amount = @TotalAmount,
                currency = @Currency,
                payment_status = CASE @PaymentStatus
                    WHEN 0 THEN 'unpaid'
                    WHEN 1 THEN 'paid'
                    WHEN 2 THEN 'failed'
                    WHEN 3 THEN 'refunded'
                END
            WHERE invoice_id = @Id::uuid";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = invoice.Id,
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            PaymentStatus = (int)invoice.PaymentStatus
        });

        return rowsAffected > 0;
    }
}

