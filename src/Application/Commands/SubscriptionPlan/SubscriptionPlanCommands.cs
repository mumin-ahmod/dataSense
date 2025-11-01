using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Application.Commands.SubscriptionPlan;

// Create Subscription Plan Command
public sealed record CreateSubscriptionPlanCommand(
    string Name,
    string Description,
    int MonthlyRequestLimit,
    decimal MonthlyPrice,
    decimal? AbroadMonthlyPrice,
    bool IsActive,
    Dictionary<string, object>? Features
) : IRequest<CreateSubscriptionPlanResponse>;

public sealed class CreateSubscriptionPlanCommandHandler : IRequestHandler<CreateSubscriptionPlanCommand, CreateSubscriptionPlanResponse>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly ILogger<CreateSubscriptionPlanCommandHandler> _logger;

    public CreateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        ILogger<CreateSubscriptionPlanCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateSubscriptionPlanResponse> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        // Check if plan with same name already exists
        var existingPlan = await _repository.GetByNameAsync(request.Name);
        if (existingPlan != null)
        {
            throw new InvalidOperationException($"A subscription plan with name '{request.Name}' already exists.");
        }

        var plan = new Domain.Models.SubscriptionPlan
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            MonthlyRequestLimit = request.MonthlyRequestLimit,
            MonthlyPrice = request.MonthlyPrice,
            AbroadMonthlyPrice = request.AbroadMonthlyPrice,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            Features = request.Features
        };

        var created = await _repository.CreateAsync(plan);
        
        _logger.LogInformation("Subscription plan created: {PlanId} - {PlanName}", created.Id, created.Name);

        return new CreateSubscriptionPlanResponse
        {
            PlanId = created.Id,
            Name = created.Name,
            Description = created.Description,
            MonthlyRequestLimit = created.MonthlyRequestLimit,
            MonthlyPrice = created.MonthlyPrice,
            AbroadMonthlyPrice = created.AbroadMonthlyPrice,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt
        };
    }
}

public class CreateSubscriptionPlanResponse
{
    public string PlanId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? AbroadMonthlyPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Update Subscription Plan Command
public sealed record UpdateSubscriptionPlanCommand(
    string PlanId,
    string Name,
    string Description,
    int MonthlyRequestLimit,
    decimal MonthlyPrice,
    decimal? AbroadMonthlyPrice,
    bool IsActive,
    Dictionary<string, object>? Features
) : IRequest<bool>;

public sealed class UpdateSubscriptionPlanCommandHandler : IRequestHandler<UpdateSubscriptionPlanCommand, bool>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly ILogger<UpdateSubscriptionPlanCommandHandler> _logger;

    public UpdateSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        ILogger<UpdateSubscriptionPlanCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _repository.GetByIdAsync(request.PlanId);
        if (plan == null)
        {
            return false;
        }

        // Check if another plan with the same name exists
        var existingPlan = await _repository.GetByNameAsync(request.Name);
        if (existingPlan != null && existingPlan.Id != request.PlanId)
        {
            throw new InvalidOperationException($"A subscription plan with name '{request.Name}' already exists.");
        }

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.MonthlyRequestLimit = request.MonthlyRequestLimit;
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.AbroadMonthlyPrice = request.AbroadMonthlyPrice;
        plan.IsActive = request.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;
        plan.Features = request.Features;

        var result = await _repository.UpdateAsync(plan);
        
        if (result)
        {
            _logger.LogInformation("Subscription plan updated: {PlanId}", request.PlanId);
        }

        return result;
    }
}

// Delete Subscription Plan Command (Soft Delete)
public sealed record DeleteSubscriptionPlanCommand(string PlanId) : IRequest<bool>;

public sealed class DeleteSubscriptionPlanCommandHandler : IRequestHandler<DeleteSubscriptionPlanCommand, bool>
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly ILogger<DeleteSubscriptionPlanCommandHandler> _logger;

    public DeleteSubscriptionPlanCommandHandler(
        ISubscriptionPlanRepository repository,
        ILogger<DeleteSubscriptionPlanCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var result = await _repository.DeleteAsync(request.PlanId);
        
        if (result)
        {
            _logger.LogInformation("Subscription plan deleted (soft delete): {PlanId}", request.PlanId);
        }

        return result;
    }
}

