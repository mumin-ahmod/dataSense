using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Models;
using DataSenseAPI.Services;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Backend API controller for SDK integration
/// Handles SQL generation and result interpretation
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class BackendController : ControllerBase
{
    private readonly IBackendSqlGeneratorService _sqlGenerator;
    private readonly IBackendResultInterpreterService _resultInterpreter;
    private readonly ISqlSafetyValidator _safetyValidator;
    private readonly ILogger<BackendController> _logger;

    public BackendController(
        IBackendSqlGeneratorService sqlGenerator,
        IBackendResultInterpreterService resultInterpreter,
        ISqlSafetyValidator safetyValidator,
        ILogger<BackendController> logger)
    {
        _sqlGenerator = sqlGenerator;
        _resultInterpreter = resultInterpreter;
        _safetyValidator = safetyValidator;
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

            // Generate SQL from natural language query
            // The service now handles safety validation and self-correction internally
            var sqlQuery = await _sqlGenerator.GenerateSqlAsync(
                request.NaturalQuery, 
                request.Schema, 
                request.DbType);

            // Final validation check (defense in depth)
            var sanitizedQuery = _safetyValidator.SanitizeQuery(sqlQuery);
            var isValid = _safetyValidator.IsSafe(sanitizedQuery);

            if (!isValid)
            {
                _logger.LogError($"Generated SQL failed final safety validation after service self-correction: {sqlQuery}");
                return StatusCode(500, new GenerateSqlResponse
                {
                    SqlQuery = string.Empty,
                    IsValid = false,
                    ErrorMessage = "Generated SQL query failed safety validation and could not be automatically corrected"
                });
            }

            _logger.LogInformation("SQL generated successfully and passed all validation layers");

            return Ok(new GenerateSqlResponse
            {
                SqlQuery = sanitizedQuery,
                IsValid = true,
                Metadata = new Dictionary<string, object>
                {
                    { "db_type", request.DbType },
                    { "tables_count", request.Schema.Tables.Count },
                    { "generated_at", DateTime.UtcNow }
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "SQL query could not be fixed after safety validation failure");
            return StatusCode(500, new GenerateSqlResponse
            {
                SqlQuery = string.Empty,
                IsValid = false,
                ErrorMessage = $"SQL generation failed: {ex.Message}"
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

            var interpretation = await _resultInterpreter.InterpretResultsAsync(request);

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

