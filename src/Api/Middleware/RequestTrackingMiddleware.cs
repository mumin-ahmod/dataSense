using System.Diagnostics;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Middleware;

public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTrackingMiddleware> _logger;

    public RequestTrackingMiddleware(
        RequestDelegate next, 
        ILogger<RequestTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, 
        IKafkaService kafkaService,
        IUsageRequestRepository usageRequestRepository,
        ISubscriptionService subscriptionService)
    {
        var stopwatch = Stopwatch.StartNew();
        var userId = context.Items["UserId"]?.ToString() ?? "anonymous";
        var apiKeyId = context.Items["ApiKeyId"]?.ToString();
        var endpoint = context.Request.Path.Value ?? "";

        // Determine request type
        var requestType = RequestType.GenerateSql;
        if (endpoint.Contains("interpret-results"))
            requestType = RequestType.InterpretResults;
        else if (endpoint.Contains("chat") || endpoint.Contains("conversation"))
            requestType = RequestType.ChatMessage;
        else if (endpoint.Contains("suggestions"))
            requestType = RequestType.WelcomeSuggestions;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // Create usage request (append-only for billing)
            var usageRequest = new UsageRequest
            {
                UserId = userId,
                ApiKeyId = apiKeyId,
                Endpoint = endpoint,
                RequestType = requestType,
                Timestamp = DateTime.UtcNow,
                StatusCode = context.Response.StatusCode,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    { "method", context.Request.Method },
                    { "userAgent", context.Request.Headers["User-Agent"].ToString() }
                }
            };

            // Save usage request asynchronously and send to Kafka for analytics
            _ = Task.Run(async () =>
            {
                try
                {
                    // Save to database (append-only)
                    await usageRequestRepository.CreateAsync(usageRequest);

                    // Send to Kafka for analytics (optional mirroring)
                    await kafkaService.ProduceAsync("datasense-usage-requests", 
                        System.Text.Json.JsonSerializer.Serialize(usageRequest));

                    // Increment subscription usage count
                    if (!string.IsNullOrEmpty(userId) && userId != "anonymous")
                    {
                        await subscriptionService.IncrementRequestCountAsync(userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging usage request");
                }
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing request");

            // Log error as usage request
            var errorRequest = new UsageRequest
            {
                UserId = userId,
                ApiKeyId = apiKeyId,
                Endpoint = endpoint,
                RequestType = requestType,
                Timestamp = DateTime.UtcNow,
                StatusCode = 500,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    { "error", ex.Message }
                }
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await usageRequestRepository.CreateAsync(errorRequest);
                }
                catch
                {
                    // Ignore errors in async logging
                }
            });

            throw;
        }
    }
}

