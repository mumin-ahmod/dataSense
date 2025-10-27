using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using DataSenseAPI.Models;
using DataSenseAPI.Services;

namespace DataSenseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly IQueryParserService _queryParserService;
    private readonly ILogger<QueryController> _logger;

    public QueryController(
        IQueryParserService queryParserService,
        ILogger<QueryController> logger)
    {
        _queryParserService = queryParserService;
        _logger = logger;
    }

    [HttpPost("parse")]
    public async Task<ActionResult<QueryResponse>> ParseQuery([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NaturalLanguageQuery))
        {
            return BadRequest(new { error = "NaturalLanguageQuery is required" });
        }

        try
        {
            var response = await _queryParserService.ParseQueryAsync(
                request.NaturalLanguageQuery);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing query");
            return StatusCode(500, new { error = "An error occurred while parsing the query", details = ex.Message });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("schema/refresh")]
    public async Task<IActionResult> RefreshSchema()
    {
        try
        {
            var schemaCache = HttpContext.RequestServices.GetRequiredService<ISchemaCacheService>();
            await schemaCache.RefreshSchemaAsync();
            return Ok(new { message = "Schema refreshed successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing schema");
            return StatusCode(500, new { error = "An error occurred while refreshing schema", details = ex.Message });
        }
    }

    [HttpGet("schema/status")]
    public IActionResult GetSchemaStatus()
    {
        try
        {
            var schemaCache = HttpContext.RequestServices.GetRequiredService<ISchemaCacheService>();
            return Ok(new { schemaLoaded = schemaCache.IsSchemaLoaded });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schema status");
            return StatusCode(500, new { error = "An error occurred", details = ex.Message });
        }
    }
}

