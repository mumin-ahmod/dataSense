using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Application.Queries.Project;

// Get Project By ID Query
public sealed record GetProjectByIdQuery(string ProjectId) : IRequest<Project?>;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Project?>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Project?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByIdAsync(request.ProjectId);
    }
}

// Get Projects By User Query
public sealed record GetProjectsByUserQuery(string UserId) : IRequest<List<Project>>;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, List<Project>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsByUserQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<List<Project>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByUserIdAsync(request.UserId);
    }
}

// Get All Projects Query (for SystemAdmin)
public sealed record GetAllProjectsQuery() : IRequest<List<Project>>;

public sealed class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, List<Project>>
{
    private readonly IProjectRepository _projectRepository;

    public GetAllProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<List<Project>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetAllAsync();
    }
}
