using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using ProjectEntity = DataSenseAPI.Domain.Models.Project;

namespace DataSenseAPI.Application.Queries.Project;

// Get Project By ID Query
public sealed record GetProjectByIdQuery(string ProjectId) : IRequest<ProjectEntity?>;

public sealed class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectEntity?>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectEntity?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByIdAsync(request.ProjectId);
    }
}

// Get Projects By User Query
public sealed record GetProjectsByUserQuery(string UserId) : IRequest<List<ProjectEntity>>;

public sealed class GetProjectsByUserQueryHandler : IRequestHandler<GetProjectsByUserQuery, List<ProjectEntity>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsByUserQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<List<ProjectEntity>> Handle(GetProjectsByUserQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetByUserIdAsync(request.UserId);
    }
}

// Get All Projects Query (for SystemAdmin)
public sealed record GetAllProjectsQuery() : IRequest<List<ProjectEntity>>;

public sealed class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, List<ProjectEntity>>
{
    private readonly IProjectRepository _projectRepository;

    public GetAllProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<List<ProjectEntity>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        return await _projectRepository.GetAllAsync();
    }
}
