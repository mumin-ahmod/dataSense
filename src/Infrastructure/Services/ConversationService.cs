using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _repository;
    private readonly IRedisService _redisService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository repository, 
        IRedisService redisService,
        ILogger<ConversationService> logger)
    {
        _repository = repository;
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<Conversation> CreateConversationAsync(
        string userId, 
        string? apiKeyId = null, 
        ConversationType type = ConversationType.Regular, 
        string? platformType = null, 
        string? externalUserId = null)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            ApiKeyId = apiKeyId,
            Type = type,
            PlatformType = platformType,
            ExternalUserId = externalUserId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        conversation = await _repository.CreateAsync(conversation);

        // Cache in Redis for quick access
        await _redisService.SaveConversationAsync(conversation.Id, conversation);

        _logger.LogInformation("Conversation created: {ConversationId} for user: {UserId}", conversation.Id, userId);

        return conversation;
    }

    public async Task<Conversation?> GetConversationByIdAsync(string conversationId)
    {
        // Try Redis first
        var cached = await _redisService.GetConversationAsync(conversationId);
        if (cached != null)
            return cached;

        // Fallback to database
        var conversation = await _repository.GetByIdAsync(conversationId);

        if (conversation != null)
        {
            await _redisService.SaveConversationAsync(conversationId, conversation);
        }

        return conversation;
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }
}

