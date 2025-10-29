using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Controller for app metadata management
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class AppMetadataController : ControllerBase
{
    private readonly IAppMetadataService _appMetadataService;
    private readonly ILogger<AppMetadataController> _logger;

    public AppMetadataController(IAppMetadataService appMetadataService, ILogger<AppMetadataController> logger)
    {
        _appMetadataService = appMetadataService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return HttpContext.Items["UserId"]?.ToString() 
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Save app metadata (project details, links, schema)
    /// </summary>
    [HttpPost("app-metadata")]
    public async Task<IActionResult> SaveAppMetadata([FromBody] SaveAppMetadataRequest request)
    {
        try
        {
            var userId = GetUserId();
            var metadata = new AppMetadata
            {
                UserId = userId,
                AppName = request.AppName,
                Description = request.Description,
                ProjectDetails = request.ProjectDetails,
                Links = request.Links,
                Schema = request.Schema
            };

            await _appMetadataService.SaveAppMetadataAsync(userId, metadata);

            return Ok(new { success = true, message = "App metadata saved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving app metadata");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

