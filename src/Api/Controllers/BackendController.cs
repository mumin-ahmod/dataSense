using Microsoft.AspNetCore.Mvc;
using MediatR;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Commands.GenerateSql;
using DataSenseAPI.Application.Commands.InterpretResults;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Security.Claims;

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
    private readonly IConversationService _conversationService;
    private readonly IRedisService _redisService;
    private readonly IAppMetadataService _appMetadataService;
    private readonly IQueryDetectionService _queryDetectionService;
    private readonly IKafkaService _kafkaService;
    private readonly IOllamaService _ollamaService;

    public BackendController(
        IMediator mediator, 
        ILogger<BackendController> logger,
        IConversationService conversationService,
        IRedisService redisService,
        IAppMetadataService appMetadataService,
        IQueryDetectionService queryDetectionService,
        IKafkaService kafkaService,
        IOllamaService ollamaService)
    {
        _mediator = mediator;
        _logger = logger;
        _conversationService = conversationService;
        _redisService = redisService;
        _appMetadataService = appMetadataService;
        _queryDetectionService = queryDetectionService;
        _kafkaService = kafkaService;
        _ollamaService = ollamaService;
    }

    private string GetUserId()
    {
        return HttpContext.Items["UserId"]?.ToString() 
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? Guid.NewGuid().ToString();
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

    /// <summary>
    /// Get welcome suggestions based on schema
    /// </summary>
    [HttpPost("welcome-suggestions")]
    public async Task<ActionResult<WelcomeSuggestionsResponse>> GetWelcomeSuggestions([FromBody] WelcomeSuggestionsRequest request)
    {
        try
        {
            var userId = GetUserId();
            var suggestions = await _appMetadataService.GenerateWelcomeSuggestionsAsync(request.Schema);

            return Ok(new WelcomeSuggestionsResponse
            {
                Success = true,
                Response = "Hello! I'm here to help. What would you like to talk about?",
                Suggestions = suggestions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating welcome suggestions");
            return StatusCode(500, new WelcomeSuggestionsResponse
            {
                Success = false,
                Response = "An error occurred",
                Suggestions = new List<string>()
            });
        }
    }

    /// <summary>
    /// Start a new conversation or continue existing one
    /// </summary>
    [HttpPost("start-conversation")]
    public async Task<ActionResult<StartConversationResponse>> StartConversation([FromBody] StartConversationRequest request)
    {
        try
        {
            var userId = request.UserId ?? GetUserId();
            var apiKeyId = HttpContext.Items["ApiKeyId"]?.ToString();

            var conversation = await _conversationService.CreateConversationAsync(
                userId, 
                apiKeyId, 
                request.Type, 
                request.PlatformType, 
                request.ExternalUserId);

            string? response = null;
            List<MessageHistoryItem>? messageHistory = null;

            // If a suggestion was provided, start the conversation with it
            if (!string.IsNullOrWhiteSpace(request.Suggestion))
            {
                // Send initial message with suggestion through Kafka
                await _kafkaService.ProduceOllamaRequestAsync(conversation.Id, request.Suggestion);
                
                // For now, return immediately (response will be handled asynchronously)
                response = "Processing your request...";
            }

            return Ok(new StartConversationResponse
            {
                Success = true,
                Message = "Conversation started",
                ConversationId = conversation.Id,
                Response = response,
                MessageHistory = messageHistory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversation");
            return StatusCode(500, new StartConversationResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                ConversationId = string.Empty
            });
        }
    }

    /// <summary>
    /// Send a message in a conversation
    /// </summary>
    [HttpPost("send-message")]
    public async Task<ActionResult<SendChatMessageResponse>> SendMessage([FromBody] SendChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConversationId))
        {
            return BadRequest(new { error = "ConversationId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required" });
        }

        try
        {
            var conversation = await _conversationService.GetConversationByIdAsync(request.ConversationId);
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            // Get chat history from Redis
            var history = await _redisService.GetChatHistoryAsync(request.ConversationId);

            // Add user message
            var userMessage = new ChatMessage
            {
                ConversationId = request.ConversationId,
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            };
            
            history.Add(userMessage);
            await _redisService.SaveChatHistoryAsync(request.ConversationId, history);

            // Check if query execution is needed
            var needsQuery = await _queryDetectionService.NeedsQueryExecutionAsync(request.Message, request.Schema);
            var queryResults = (InterpretationData?)null;

            // Get app metadata for links
            var appMetadata = await _appMetadataService.GetAppMetadataAsync(conversation.UserId);
            var links = appMetadata?.Links;

            // Send to Kafka for async processing via Ollama
            Dictionary<string, object>? metadata = new Dictionary<string, object>
            {
                { "requiresQuery", needsQuery },
                { "schema", request.Schema != null ? "provided" : "none" }
            };

            await _kafkaService.ProduceOllamaRequestAsync(request.ConversationId, request.Message, metadata);

            // For now, return a placeholder response
            // In production, the Kafka consumer will process this and update the conversation
            var assistantMessage = new ChatMessage
            {
                ConversationId = request.ConversationId,
                Role = "assistant",
                Content = "I'm processing your request...",
                Timestamp = DateTime.UtcNow
            };

            history.Add(assistantMessage);
            await _redisService.SaveChatHistoryAsync(request.ConversationId, history);

            var messageHistory = history.Select(m => new MessageHistoryItem
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp,
                MessageId = m.Id
            }).ToList();

            return Ok(new SendChatMessageResponse
            {
                Success = true,
                Message = "Message received",
                ConversationId = request.ConversationId,
                Response = assistantMessage.Content,
                MessageHistory = messageHistory,
                Links = links,
                RequiresQueryExecution = needsQuery,
                QueryResults = queryResults
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new SendChatMessageResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                ConversationId = request.ConversationId,
                Response = string.Empty
            });
        }
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
                "POST /api/v1/backend/app-metadata"
            }
        });
    }
}


