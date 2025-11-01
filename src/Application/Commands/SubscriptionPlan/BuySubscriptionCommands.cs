using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Application.Commands.SubscriptionPlan;

// Buy Subscription Command - Initiates payment
public sealed record BuySubscriptionCommand(
    string UserId,
    string PlanId,
    string? PaymentProvider = null,
    bool IsAbroad = false
) : IRequest<BuySubscriptionResponse>;

public sealed class BuySubscriptionCommandHandler : IRequestHandler<BuySubscriptionCommand, BuySubscriptionResponse>
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBillingEventRepository _billingEventRepository;
    private readonly ILogger<BuySubscriptionCommandHandler> _logger;

    public BuySubscriptionCommandHandler(
        ISubscriptionPlanRepository planRepository,
        IUserSubscriptionRepository subscriptionRepository,
        IPaymentRepository paymentRepository,
        IBillingEventRepository billingEventRepository,
        ILogger<BuySubscriptionCommandHandler> logger)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _paymentRepository = paymentRepository;
        _billingEventRepository = billingEventRepository;
        _logger = logger;
    }

    public async Task<BuySubscriptionResponse> Handle(BuySubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Get subscription plan
        var plan = await _planRepository.GetByIdAsync(request.PlanId);
        if (plan == null || !plan.IsActive)
        {
            throw new InvalidOperationException("Subscription plan not found or is not active");
        }

        // Calculate price based on location
        var amount = request.IsAbroad && plan.AbroadMonthlyPrice.HasValue
            ? plan.AbroadMonthlyPrice.Value
            : plan.MonthlyPrice;

        // Create billing event with 'cart' or 'initiated' status
        var billingEventId = Guid.NewGuid().ToString();
        var idempotencyKey = $"{request.UserId}_{request.PlanId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var billingEvent = new BillingEvent
        {
            Id = billingEventId,
            SubscriptionId = string.Empty, // Will be set after subscription is created
            EventType = BillingEventType.Cart,
            IdempotencyKey = idempotencyKey,
            EstimatedCost = amount,
            CreatedAt = DateTime.UtcNow
        };

        // Create pending payment
        var paymentId = Guid.NewGuid().ToString();
        var payment = new Payment
        {
            Id = paymentId,
            UserId = request.UserId,
            SubscriptionId = string.Empty, // Will be set after subscription is created
            PaymentProvider = request.PaymentProvider,
            Amount = amount,
            Currency = "USD",
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create pending subscription (will be activated after payment)
        var subscriptionId = Guid.NewGuid().ToString();
        var subscription = new UserSubscription
        {
            Id = subscriptionId,
            UserId = request.UserId,
            SubscriptionPlanId = request.PlanId,
            StartDate = DateTime.UtcNow,
            IsActive = false, // Will be activated after payment
            UsedRequestsThisMonth = 0,
            LastResetDate = DateTime.UtcNow
        };

        // Deactivate existing subscription if any
        var existingSubscription = await _subscriptionRepository.GetByUserIdAsync(request.UserId);
        if (existingSubscription != null)
        {
            await _subscriptionRepository.DeactivateAsync(existingSubscription.Id);
        }

        // Save subscription
        await _subscriptionRepository.CreateAsync(subscription);

        // Update payment and billing event with subscription ID
        payment.SubscriptionId = subscriptionId;
        billingEvent.SubscriptionId = subscriptionId;

        // Save payment
        await _paymentRepository.CreateAsync(payment);

        // Save billing event
        await _billingEventRepository.CreateAsync(billingEvent);

        _logger.LogInformation("Subscription purchase initiated: UserId={UserId}, PlanId={PlanId}, PaymentId={PaymentId}", 
            request.UserId, request.PlanId, paymentId);

        return new BuySubscriptionResponse
        {
            PaymentId = paymentId,
            SubscriptionId = subscriptionId,
            BillingEventId = billingEventId,
            Amount = amount,
            Currency = "USD",
            Status = "pending"
        };
    }
}

public class BuySubscriptionResponse
{
    public string PaymentId { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string BillingEventId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "pending";
}

// Process Payment Command - Handles successful payment
public sealed record ProcessPaymentCommand(
    string PaymentId,
    string TransactionId,
    string PaymentProvider
) : IRequest<bool>;

public sealed class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, bool>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBillingEventRepository _billingEventRepository;
    private readonly IUsageRecordRepository _usageRecordRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository,
        IInvoiceRepository invoiceRepository,
        IBillingEventRepository billingEventRepository,
        IUsageRecordRepository usageRecordRepository,
        IApiKeyRepository apiKeyRepository,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _invoiceRepository = invoiceRepository;
        _billingEventRepository = billingEventRepository;
        _usageRecordRepository = usageRecordRepository;
        _apiKeyRepository = apiKeyRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Get payment
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found: {PaymentId}", request.PaymentId);
            return false;
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            _logger.LogWarning("Payment already processed: {PaymentId}, Status={Status}", request.PaymentId, payment.Status);
            return false;
        }

        // Update payment status to completed
        payment.Status = PaymentStatus.Completed;
        payment.TransactionId = request.TransactionId;
        payment.PaymentProvider = request.PaymentProvider;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment);

        // Activate subscription
        var subscription = await _subscriptionRepository.GetByIdAsync(payment.SubscriptionId);
        if (subscription == null)
        {
            _logger.LogError("Subscription not found: {SubscriptionId}", payment.SubscriptionId);
            return false;
        }

        subscription.IsActive = true;
        subscription.StartDate = DateTime.UtcNow;
        subscription.LastResetDate = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription);

        // Get subscription plan
        var plan = await _planRepository.GetByIdAsync(subscription.SubscriptionPlanId);
        if (plan == null)
        {
            _logger.LogError("Subscription plan not found: {PlanId}", subscription.SubscriptionPlanId);
            return false;
        }

        // Create invoice
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{payment.Id.Substring(0, 8).ToUpper()}";
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddMonths(1);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = subscription.Id,
            UserId = payment.UserId,
            InvoiceNumber = invoiceNumber,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalAmount = payment.Amount,
            Currency = payment.Currency,
            PaymentStatus = InvoicePaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        await _invoiceRepository.CreateAsync(invoice);

        // Create billing event for subscription
        var billingEvent = new BillingEvent
        {
            Id = Guid.NewGuid().ToString(),
            SubscriptionId = subscription.Id,
            EventType = BillingEventType.Subscription,
            IdempotencyKey = $"payment_{payment.Id}",
            EstimatedCost = payment.Amount,
            CreatedAt = DateTime.UtcNow
        };

        await _billingEventRepository.CreateAsync(billingEvent);

        // Initialize usage record for the user
        var today = DateTime.UtcNow.Date;
        var existingUsageRecord = await _usageRecordRepository.GetByUserIdAndDateAsync(payment.UserId, today);

        if (existingUsageRecord == null)
        {
            // Create initial usage record with full monthly limit
            var usageRecord = new UsageRecord
            {
                Id = Guid.NewGuid().ToString(),
                UserId = payment.UserId,
                RequestType = RequestType.GenerateSql, // Default to first request type
                RequestCount = 0,
                RequestLeft = plan.MonthlyRequestLimit,
                Date = today
            };

            await _usageRecordRepository.CreateAsync(usageRecord);
        }

        _logger.LogInformation("Payment processed successfully: PaymentId={PaymentId}, SubscriptionId={SubscriptionId}, InvoiceNumber={InvoiceNumber}",
            request.PaymentId, subscription.Id, invoiceNumber);

        return true;
    }
}

// Generate API Key Command
public sealed record GenerateApiKeyCommand(
    string UserId,
    string Name,
    Dictionary<string, object>? Metadata = null
) : IRequest<GenerateApiKeyResponse>;

public sealed class GenerateApiKeyCommandHandler : IRequestHandler<GenerateApiKeyCommand, GenerateApiKeyResponse>
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IUserSubscriptionRepository _subscriptionRepository;
    private readonly ILogger<GenerateApiKeyCommandHandler> _logger;

    public GenerateApiKeyCommandHandler(
        IApiKeyService apiKeyService,
        IApiKeyRepository apiKeyRepository,
        IUserSubscriptionRepository subscriptionRepository,
        ILogger<GenerateApiKeyCommandHandler> logger)
    {
        _apiKeyService = apiKeyService;
        _apiKeyRepository = apiKeyRepository;
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    public async Task<GenerateApiKeyResponse> Handle(GenerateApiKeyCommand request, CancellationToken cancellationToken)
    {
        // Check if user has active subscription
        var subscription = await _subscriptionRepository.GetByUserIdAsync(request.UserId);
        if (subscription == null || !subscription.IsActive)
        {
            throw new InvalidOperationException("User does not have an active subscription. Please subscribe first.");
        }

        // Generate API key
        var apiKey = await _apiKeyService.GenerateApiKeyAsync(request.UserId, request.Name, request.Metadata);

        // Get the API key details from repository
        var apiKeys = await _apiKeyRepository.GetByUserIdAsync(request.UserId);
        var generatedKey = apiKeys.FirstOrDefault(k => k.Name == request.Name);

        _logger.LogInformation("API key generated for user: UserId={UserId}, Name={Name}", request.UserId, request.Name);

        return new GenerateApiKeyResponse
        {
            ApiKey = apiKey,
            KeyId = generatedKey?.Id ?? string.Empty,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class GenerateApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

