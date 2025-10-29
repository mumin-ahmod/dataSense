using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Commands.GenerateSql;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Controller for SQL generation from natural language queries
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class SqlGenerationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SqlGenerationController> _logger;

    public SqlGenerationController(IMediator mediator, ILogger<SqlGenerationController> logger)
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

            var sqlQuery = await _mediator.Send(new GenerateSqlCommand(request.NaturalQuery, request.Schema!, request.DbType));
            _logger.LogInformation("SQL generated successfully and passed all validation layers");
            
            return Ok(new GenerateSqlResponse
            {
                SqlQuery = sqlQuery,
                IsValid = true,
                Metadata = new Dictionary<string, object>
                {
                    { "db_type", request.DbType },
                    { "tables_count", request.Schema?.Tables.Count ?? 0 },
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
}

