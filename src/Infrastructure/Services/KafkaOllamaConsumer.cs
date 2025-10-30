using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DataSenseAPI.Infrastructure.Services;

public class KafkaOllamaConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IOllamaService _ollamaService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaOllamaConsumer> _logger;
    private const string OllamaRequestsTopic = "datasense-ollama-requests";

    public KafkaOllamaConsumer(
        IOllamaService ollamaService,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaOllamaConsumer> logger,
        IConfiguration configuration)
    {
        _ollamaService = ollamaService;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var bootstrapServers =
            configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "datasense-ollama-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnablePartitionEof = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay consumer startup to not block Kestrel binding
        await Task.Delay(2000, stoppingToken);

        try
        {
            _consumer.Subscribe(OllamaRequestsTopic);
            _logger.LogInformation("Kafka consumer started, listening to topic: {Topic}", OllamaRequestsTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to Kafka topic. Consumer will not process messages.");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use timeout to prevent indefinite blocking if Kafka is unavailable
                    var result = _consumer.Consume(TimeSpan.FromSeconds(5));

                    if (result == null)
                    {
                        // Timeout reached, no message received
                        continue;
                    }

                    if (result.IsPartitionEOF)
                    {
                        _logger.LogDebug("Reached end of partition");
                        continue;
                    }

                    _logger.LogDebug("Received message from Kafka");

                    // Process message asynchronously
                    _ = Task.Run(async () => await ProcessMessageAsync(result.Message.Value, result.Message.Headers), stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                    // Wait before retrying to avoid tight error loops
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        finally
        {
            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Kafka consumer");
            }
        }
    }

    private async Task ProcessMessageAsync(string message, Headers? headers)
    {
        try
        {
            var request = JsonSerializer.Deserialize<JsonElement>(message);
            
            if (!request.TryGetProperty("conversationId", out var conversationIdElement))
            {
                _logger.LogWarning("Message missing conversationId");
                return;
            }

            var conversationId = conversationIdElement.GetString();
            if (string.IsNullOrEmpty(conversationId))
            {
                _logger.LogWarning("Invalid conversationId in message");
                return;
            }

            if (!request.TryGetProperty("prompt", out var promptElement))
            {
                _logger.LogWarning("Message missing prompt");
                return;
            }

            var prompt = promptElement.GetString() ?? "";
            var metadata = request.TryGetProperty("metadata", out var metadataElement) 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(metadataElement.GetRawText()) 
                : null;

            // Get conversation and chat history
            using var scope = _scopeFactory.CreateScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();
            var appMetadataService = scope.ServiceProvider.GetRequiredService<IAppMetadataService>();
            var queryDetectionService = scope.ServiceProvider.GetRequiredService<IQueryDetectionService>();

            var conversation = await conversationService.GetConversationByIdAsync(conversationId);
            if (conversation == null)
            {
                _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
                return;
            }

            var history = await redisService.GetChatHistoryAsync(conversationId);
            var appMetadata = await appMetadataService.GetAppMetadataAsync(conversation.UserId);

            // Build context-aware prompt
            var contextPrompt = BuildContextPrompt(prompt, history, appMetadata, metadata);

            // Query Ollama
            var response = await _ollamaService.QueryLLMAsync(contextPrompt);

            // Save assistant response to history
            var assistantMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow,
                Metadata = metadata
            };

            await redisService.AddMessageToHistoryAsync(conversationId, assistantMessage);

            // Update conversation timestamp
            conversation.UpdatedAt = DateTime.UtcNow;
            await redisService.SaveConversationAsync(conversationId, conversation);

            _logger.LogInformation("Processed Ollama request for conversation: {ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Ollama message from Kafka");
        }
    }

    private string BuildContextPrompt(
        string userMessage, 
        List<ChatMessage> history, 
        AppMetadata? appMetadata,
        Dictionary<string, object>? metadata)
    {
        var prompt = new System.Text.StringBuilder();

        // Add app context if available
        if (appMetadata != null)
        {
            if (!string.IsNullOrEmpty(appMetadata.Description))
            {
                prompt.AppendLine($"Application context: {appMetadata.Description}");
            }
            
            if (appMetadata.Links != null && appMetadata.Links.Any())
            {
                prompt.AppendLine("Relevant links:");
                foreach (var link in appMetadata.Links)
                {
                    prompt.AppendLine($"- {link.Title}: {link.Url}");
                }
            }
        }

        // Add recent chat history (last 10 messages)
        var recentHistory = history.TakeLast(10).ToList();
        if (recentHistory.Any())
        {
            prompt.AppendLine("\nConversation history:");
            foreach (var msg in recentHistory)
            {
                prompt.AppendLine($"{msg.Role}: {msg.Content}");
            }
        }

        prompt.AppendLine($"\nUser: {userMessage}");
        prompt.AppendLine("Assistant:");

        return prompt.ToString();
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

