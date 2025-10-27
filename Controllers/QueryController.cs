using Microsoft.AspNetCore.Mvc;
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
                request.NaturalLanguageQuery,
                request.ConnectionName);

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
}

