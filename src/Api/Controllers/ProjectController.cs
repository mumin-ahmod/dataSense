using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DataSenseAPI.Application.Commands.Project;
using DataSenseAPI.Application.Queries.Project;
using DataSenseAPI.Application.Abstractions;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Project management controller
/// </summary>
[ApiController]
[Route("api/v1/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(IMediator mediator, ILogger<ProjectController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("UserId")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    private bool IsSystemAdmin()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        return roles.Contains("SystemAdmin");
    }

    private async Task<bool> VerifyOwnershipAsync(string projectId)
    {
        if (IsSystemAdmin())
        {
            return true;
        }

        var project = await _mediator.Send(new GetProjectByIdQuery(projectId));
        if (project == null)
        {
            return false;
        }

        return project.UserId == GetUserId();
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateProjectResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        try
        {
            var userId = GetUserId();
            var command = new CreateProjectCommand(
                request.Name,
                request.Description,
                request.MessageChannel,
                request.ChannelNumber,
                userId
            );

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetProject), new { id = result.ProjectId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, new { error = "Failed to create project", message = ex.Message });
        }
    }

    /// <summary>
    /// Get project by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Domain.Models.Project>> GetProject(string id)
    {
        var project = await _mediator.Send(new GetProjectByIdQuery(id));
        
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        // Verify ownership
        if (!await VerifyOwnershipAsync(id))
        {
            return Forbid();
        }

        // Remove sensitive data from response
        var response = new
        {
            project.Id,
            project.UserId,
            project.Name,
            project.Description,
            project.MessageChannel,
            project.ChannelNumber,
            project.IsActive,
            project.CreatedAt,
            project.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get all projects for current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Domain.Models.Project>>> GetProjects()
    {
        try
        {
            var userId = GetUserId();
            List<Domain.Models.Project> projects;

            if (IsSystemAdmin())
            {
                projects = await _mediator.Send(new GetAllProjectsQuery());
            }
            else
            {
                projects = await _mediator.Send(new GetProjectsByUserQuery(userId));
            }

            // Remove sensitive data from response
            var response = projects.Select(p => new
            {
                p.Id,
                p.UserId,
                p.Name,
                p.Description,
                p.MessageChannel,
                p.ChannelNumber,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects");
            return StatusCode(500, new { error = "Failed to get projects", message = ex.Message });
        }
    }

    /// <summary>
    /// Update project information (not API key)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(string id, [FromBody] UpdateProjectRequest request)
    {
        try
        {
            // Verify ownership
            if (!await VerifyOwnershipAsync(id))
            {
                return Forbid();
            }

            var command = new UpdateProjectCommand(
                id,
                request.Name,
                request.Description,
                request.MessageChannel,
                request.ChannelNumber
            );

            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { error = "Project not found" });
            }

            return Ok(new { success = true, message = "Project updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return StatusCode(500, new { error = "Failed to update project", message = ex.Message });
        }
    }

    /// <summary>
    /// Soft delete project (toggle active status)
    /// </summary>
    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string id)
    {
        try
        {
            // Verify ownership
            if (!await VerifyOwnershipAsync(id))
            {
                return Forbid();
            }

            var command = new SoftDeleteProjectCommand(id);
            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { error = "Project not found" });
            }

            return Ok(new { success = true, message = "Project active status toggled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for project {ProjectId}", id);
            return StatusCode(500, new { error = "Failed to toggle active status", message = ex.Message });
        }
    }

    /// <summary>
    /// Hard delete project (permanent deletion)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id)
    {
        try
        {
            // Verify ownership
            if (!await VerifyOwnershipAsync(id))
            {
                return Forbid();
            }

            var command = new HardDeleteProjectCommand(id);
            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { error = "Project not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return StatusCode(500, new { error = "Failed to delete project", message = ex.Message });
        }
    }

    /// <summary>
    /// Generate a new project key
    /// </summary>
    [HttpPost("{id}/generate-key")]
    public async Task<ActionResult<GenerateProjectKeyResponse>> GenerateProjectKey(string id)
    {
        try
        {
            // Verify ownership
            if (!await VerifyOwnershipAsync(id))
            {
                return Forbid();
            }

            var command = new GenerateProjectKeyCommand(id);
            var result = await _mediator.Send(command);
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project key for project {ProjectId}", id);
            return StatusCode(500, new { error = "Failed to generate project key", message = ex.Message });
        }
    }

    /// <summary>
    /// Update project key
    /// </summary>
    [HttpPut("{id}/key")]
    public async Task<IActionResult> UpdateProjectKey(string id, [FromBody] UpdateProjectKeyRequest request)
    {
        try
        {
            // Verify ownership
            if (!await VerifyOwnershipAsync(id))
            {
                return Forbid();
            }

            var command = new UpdateProjectKeyCommand(id, request.NewProjectKey);
            var result = await _mediator.Send(command);
            
            if (!result.Success)
            {
                return BadRequest(new { error = "Failed to update project key" });
            }

            return Ok(new { success = true, message = "Project key updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project key for project {ProjectId}", id);
            return StatusCode(500, new { error = "Failed to update project key", message = ex.Message });
        }
    }
}

// Request DTOs
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MessageChannel { get; set; }
    public string? ChannelNumber { get; set; }
}

public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MessageChannel { get; set; }
    public string? ChannelNumber { get; set; }
}

public class UpdateProjectKeyRequest
{
    public string NewProjectKey { get; set; } = string.Empty;
}
