using Microsoft.AspNetCore.Mvc;
using DataSenseAPI.Api.Contracts;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Controller for chat/conversation functionality
/// </summary>
[ApiController]
[Route("api/v1/backend")]
public class ChatController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IRedisService _redisService;
    private readonly IAppMetadataService _appMetadataService;
    private readonly IQueryDetectionService _queryDetectionService;
    private readonly IKafkaService _kafkaService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IConversationService conversationService,
        IRedisService redisService,
        IAppMetadataService appMetadataService,
        IQueryDetectionService queryDetectionService,
        IKafkaService kafkaService,
        ILogger<ChatController> logger)
    {
        _conversationService = conversationService;
        _redisService = redisService;
        _appMetadataService = appMetadataService;
        _queryDetectionService = queryDetectionService;
        _kafkaService = kafkaService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return HttpContext.Items["UserId"]?.ToString() 
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? Guid.NewGuid().ToString();
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
}

