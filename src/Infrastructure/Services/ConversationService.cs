using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using DataSenseAPI.Infrastructure.AppDb;

namespace DataSenseAPI.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly ApplicationDbContext _context;
    private readonly IRedisService _redisService;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        ApplicationDbContext context, 
        IRedisService redisService,
        ILogger<ConversationService> logger)
    {
        _context = context;
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

        _context.Set<Conversation>().Add(conversation);
        await _context.SaveChangesAsync();

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
        var conversation = await _context.Set<Conversation>()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsActive);

        if (conversation != null)
        {
            await _redisService.SaveConversationAsync(conversationId, conversation);
        }

        return conversation;
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
    {
        return await _context.Set<Conversation>()
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();
    }
}

