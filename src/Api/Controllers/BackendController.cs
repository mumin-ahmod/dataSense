using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Commands.GenerateSql;
using DataSenseAPI.Application.Commands.InterpretResults;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Backend API controller for SDK integration
/// Handles SQL generation and result interpretation
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class BackendController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BackendController> _logger;

    public BackendController(IMediator mediator, ILogger<BackendController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generate SQL from natural language query using provided schema
    /// Called by SDK client
    /// </summary>
    /// <param name="request">Request containing natural query, schema, and db type</param>
    /// <returns>Generated and validated SQL query</returns>
    [HttpPost("generate-sql")]
    public async Task<ActionResult<GenerateSqlResponse>> GenerateSql([FromBody] GenerateSqlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NaturalQuery))
        {
            return BadRequest(new { error = "NaturalQuery is required" });
        }

        if (request.Schema == null || !request.Schema.Tables.Any())
        {
            return BadRequest(new { error = "Schema with at least one table is required" });
        }

        try
        {
            _logger.LogInformation($"Generating SQL for: {request.NaturalQuery} (DB: {request.DbType})");

            var sqlQuery = await _mediator.Send(new GenerateSqlCommand(request.NaturalQuery, request.Schema, request.DbType));
            _logger.LogInformation("SQL generated successfully and passed all validation layers");
            return Ok(new GenerateSqlResponse
            {
                SqlQuery = sqlQuery,
                IsValid = true,
                Metadata = new Dictionary<string, object>
                {
                    { "db_type", request.DbType },
                    { "tables_count", request.Schema.Tables.Count },
                    { "generated_at", DateTime.UtcNow }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating SQL");
            return StatusCode(500, new GenerateSqlResponse
            {
                SqlQuery = string.Empty,
                IsValid = false,
                ErrorMessage = $"An error occurred while generating SQL: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Interpret query results and provide natural language summary
    /// Called by SDK client after executing SQL locally
    /// </summary>
    /// <param name="request">Request containing original query, SQL, and results</param>
    /// <returns>Natural language interpretation of results</returns>
    [HttpPost("interpret-results")]
    public async Task<ActionResult<InterpretResultsResponse>> InterpretResults([FromBody] InterpretResultsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalQuery))
        {
            return BadRequest(new { error = "OriginalQuery is required" });
        }

        if (string.IsNullOrWhiteSpace(request.SqlQuery))
        {
            return BadRequest(new { error = "SqlQuery is required" });
        }

        if (request.Results == null)
        {
            return BadRequest(new { error = "Results are required" });
        }

        try
        {
            _logger.LogInformation($"Interpreting results for query: {request.OriginalQuery}");

            var interpretation = await _mediator.Send(new InterpretResultsCommand(request.OriginalQuery, request.SqlQuery, request.Results));

            _logger.LogInformation("Results interpreted successfully");

            return Ok(new InterpretResultsResponse
            {
                Interpretation = interpretation,
                IsValid = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interpreting results");
            return StatusCode(500, new InterpretResultsResponse
            {
                IsValid = false,
                ErrorMessage = $"An error occurred while interpreting results: {ex.Message}"
            });
        }
    }

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
                "POST /api/v1/backend/interpret-results"
            }
        });
    }
}


