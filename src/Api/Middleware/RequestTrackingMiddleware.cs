using System.Diagnostics;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Middleware;

public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTrackingMiddleware> _logger;
    private readonly IKafkaService _kafkaService;

    public RequestTrackingMiddleware(
        RequestDelegate next, 
        ILogger<RequestTrackingMiddleware> logger,
        IKafkaService kafkaService)
    {
        _next = next;
        _logger = logger;
        _kafkaService = kafkaService;
    }

    public async Task InvokeAsync(HttpContext context)
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

            // Log request asynchronously via Kafka
            var requestLog = new RequestLog
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

            _ = Task.Run(async () =>
            {
                try
                {
                    await _kafkaService.ProduceRequestLogAsync(requestLog);

                    // Also create pricing record (count requests per user/day)
                    var pricingRecord = new PricingRecord
                    {
                        UserId = userId,
                        RequestType = requestType,
                        RequestCount = 1,
                        Cost = CalculateCost(requestType),
                        Date = DateTime.UtcNow.Date
                    };

                    await _kafkaService.ProducePricingRecordAsync(pricingRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error logging request to Kafka");
                }
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing request");

            var errorLog = new RequestLog
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
                    await _kafkaService.ProduceRequestLogAsync(errorLog);
                }
                catch
                {
                    // Ignore errors in async logging
                }
            });

            throw;
        }
    }

    private static decimal CalculateCost(RequestType requestType)
    {
        // Simple pricing model - can be enhanced
        return requestType switch
        {
            RequestType.GenerateSql => 0.001m,
            RequestType.InterpretResults => 0.002m,
            RequestType.ChatMessage => 0.003m,
            RequestType.WelcomeSuggestions => 0.0005m,
            _ => 0.001m
        };
    }
}

