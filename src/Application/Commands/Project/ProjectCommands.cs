using MediatR;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Text;

namespace DataSenseAPI.Application.Commands.Project;

// Create Project Command
public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    string? MessageChannel,
    string? ChannelNumber,
    string UserId
) : IRequest<CreateProjectResponse>;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<CreateProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // Generate project key and hash
        var keyBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        var projectKey = Convert.ToBase64String(keyBytes);
        var keyHash = HashProjectKey(projectKey);

        var project = new Project
        {
            Id = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            Name = request.Name,
            Description = request.Description,
            MessageChannel = request.MessageChannel ?? "api",
            ChannelNumber = request.ChannelNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ProjectKeyHash = keyHash
        };

        var created = await _projectRepository.CreateAsync(project);
        
        _logger.LogInformation("Project created: {ProjectId} by user {UserId}", created.Id, request.UserId);

        return new CreateProjectResponse
        {
            ProjectId = created.Id,
            ProjectKey = projectKey
        };
    }

    private static string HashProjectKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class CreateProjectResponse
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}

// Update Project Command
public sealed record UpdateProjectCommand(
    string ProjectId,
    string Name,
    string? Description,
    string? MessageChannel,
    string? ChannelNumber
) : IRequest<bool>;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<UpdateProjectCommandHandler> _logger;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<UpdateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            return false;
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.MessageChannel = request.MessageChannel ?? project.MessageChannel;
        project.ChannelNumber = request.ChannelNumber;

        var result = await _projectRepository.UpdateAsync(project);
        
        if (result)
        {
            _logger.LogInformation("Project updated: {ProjectId}", request.ProjectId);
        }

        return result;
    }
}

// Soft Delete (Toggle Active) Command
public sealed record SoftDeleteProjectCommand(string ProjectId) : IRequest<bool>;

public sealed class SoftDeleteProjectCommandHandler : IRequestHandler<SoftDeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<SoftDeleteProjectCommandHandler> _logger;

    public SoftDeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<SoftDeleteProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(SoftDeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var result = await _projectRepository.ToggleActiveAsync(request.ProjectId);
        
        if (result)
        {
            _logger.LogInformation("Project soft deleted (active toggled): {ProjectId}", request.ProjectId);
        }

        return result;
    }
}

// Hard Delete Command
public sealed record HardDeleteProjectCommand(string ProjectId) : IRequest<bool>;

public sealed class HardDeleteProjectCommandHandler : IRequestHandler<HardDeleteProjectCommand, bool>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<HardDeleteProjectCommandHandler> _logger;

    public HardDeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        ILogger<HardDeleteProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(HardDeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var result = await _projectRepository.DeleteAsync(request.ProjectId);
        
        if (result)
        {
            _logger.LogInformation("Project hard deleted: {ProjectId}", request.ProjectId);
        }

        return result;
    }
}

// Generate Project Key Command
public sealed record GenerateProjectKeyCommand(string ProjectId) : IRequest<GenerateProjectKeyResponse>;

public sealed class GenerateProjectKeyCommandHandler : IRequestHandler<GenerateProjectKeyCommand, GenerateProjectKeyResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GenerateProjectKeyCommandHandler> _logger;

    public GenerateProjectKeyCommandHandler(
        IProjectRepository projectRepository,
        ILogger<GenerateProjectKeyCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<GenerateProjectKeyResponse> Handle(GenerateProjectKeyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found");
        }

        // Generate new key
        var keyBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        var projectKey = Convert.ToBase64String(keyBytes);
        var keyHash = HashProjectKey(projectKey);

        // Update project with new key hash
        var updated = await _projectRepository.UpdateProjectKeyAsync(request.ProjectId, keyHash);
        
        if (updated)
        {
            _logger.LogInformation("Project key generated for project: {ProjectId}", request.ProjectId);
            return new GenerateProjectKeyResponse { ProjectKey = projectKey };
        }

        throw new InvalidOperationException("Failed to generate project key");
    }

    private static string HashProjectKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class GenerateProjectKeyResponse
{
    public string ProjectKey { get; set; } = string.Empty;
}

// Update Project Key Command
public sealed record UpdateProjectKeyCommand(string ProjectId, string NewProjectKey) : IRequest<UpdateProjectKeyResponse>;

public sealed class UpdateProjectKeyCommandHandler : IRequestHandler<UpdateProjectKeyCommand, UpdateProjectKeyResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<UpdateProjectKeyCommandHandler> _logger;

    public UpdateProjectKeyCommandHandler(
        IProjectRepository projectRepository,
        ILogger<UpdateProjectKeyCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<UpdateProjectKeyResponse> Handle(UpdateProjectKeyCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found");
        }

        var keyHash = HashProjectKey(request.NewProjectKey);
        var updated = await _projectRepository.UpdateProjectKeyAsync(request.ProjectId, keyHash);
        
        if (updated)
        {
            _logger.LogInformation("Project key updated for project: {ProjectId}", request.ProjectId);
            return new UpdateProjectKeyResponse { Success = true };
        }

        throw new InvalidOperationException("Failed to update project key");
    }

    private static string HashProjectKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class UpdateProjectKeyResponse
{
    public bool Success { get; set; }
}
