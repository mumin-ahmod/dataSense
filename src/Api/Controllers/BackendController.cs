using Microsoft.AspNetCore.Mvc;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Backend API health check controller
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class BackendController : ControllerBase
{

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            version = "1.0",
            timestamp = DateTime.UtcNow,
            endpoints = new[]
            {
                "POST /api/v1/backend/generate-sql",
                "POST /api/v1/backend/interpret-results",
                "POST /api/v1/backend/welcome-suggestions",
                "POST /api/v1/backend/start-conversation",
                "POST /api/v1/backend/send-message",
                "POST /api/v1/backend/app-metadata",
                "GET /api/v1/backend/health"
            }
        });
    }
}


