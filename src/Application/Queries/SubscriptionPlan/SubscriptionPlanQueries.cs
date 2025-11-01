using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Queries.SubscriptionPlan;

// Get Subscription Plan By ID Query
public sealed record GetSubscriptionPlanByIdQuery(string PlanId) : IRequest<Domain.Models.SubscriptionPlan?>;

public sealed class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, Domain.Models.SubscriptionPlan?>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetSubscriptionPlanByIdQueryHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<Domain.Models.SubscriptionPlan?> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.PlanId);
    }
}

// Get All Subscription Plans Query
public sealed record GetAllSubscriptionPlansQuery() : IRequest<List<Domain.Models.SubscriptionPlan>>;

public sealed class GetAllSubscriptionPlansQueryHandler : IRequestHandler<GetAllSubscriptionPlansQuery, List<Domain.Models.SubscriptionPlan>>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetAllSubscriptionPlansQueryHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Domain.Models.SubscriptionPlan>> Handle(GetAllSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllAsync();
    }
}

// Get All Active Subscription Plans Query (Public)
public sealed record GetActiveSubscriptionPlansQuery() : IRequest<List<Domain.Models.SubscriptionPlan>>;

public sealed class GetActiveSubscriptionPlansQueryHandler : IRequestHandler<GetActiveSubscriptionPlansQuery, List<Domain.Models.SubscriptionPlan>>
{
    private readonly ISubscriptionPlanRepository _repository;

    public GetActiveSubscriptionPlansQueryHandler(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Domain.Models.SubscriptionPlan>> Handle(GetActiveSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        // GetAllAsync already filters by is_active = true
        return await _repository.GetAllAsync();
    }
}

