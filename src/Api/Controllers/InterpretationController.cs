using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Commands.InterpretResults;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Controller for interpreting SQL query results
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class InterpretationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InterpretationController> _logger;

    public InterpretationController(IMediator mediator, ILogger<InterpretationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Interpret query results and provide natural language summary
    /// Called by SDK client after executing SQL locally
    /// Supports additional context parameter
    /// </summary>
    /// <param name="request">Request containing original query, SQL, results, and optional context</param>
    /// <returns>Natural language interpretation of results</returns>
    [HttpPost("interpret-results")]
    public async Task<ActionResult<InterpretResultsResponse>> InterpretResults([FromBody] InterpretResultsRequestExtended request)
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

            InterpretationData interpretation;
            if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
            {
                interpretation = await _mediator.Send(new InterpretResultsExtendedCommand(
                    request.OriginalQuery, 
                    request.SqlQuery, 
                    request.Results, 
                    request.AdditionalContext));
            }
            else
            {
                interpretation = await _mediator.Send(new InterpretResultsCommand(
                    request.OriginalQuery, 
                    request.SqlQuery, 
                    request.Results));
            }

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
}

