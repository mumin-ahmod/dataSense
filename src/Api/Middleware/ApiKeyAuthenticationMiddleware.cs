using System.Security.Claims;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly ISubscriptionService _subscriptionService;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next, 
        ILogger<ApiKeyAuthenticationMiddleware> logger,
        ISubscriptionService subscriptionService)
    {
        _next = next;
        _logger = logger;
        _subscriptionService = subscriptionService;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        // Skip authentication for health check and public endpoints
        if (context.Request.Path.StartsWithSegments("/api/v1/backend/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        string? apiKey = null;

        // Try to get API key from Authorization header
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = authValue.Substring("Bearer ".Length).Trim();
            }
            else if (authValue.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = authValue.Substring("ApiKey ".Length).Trim();
            }
        }

        // Try to get from query string as fallback
        if (string.IsNullOrEmpty(apiKey) && context.Request.Query.TryGetValue("apiKey", out var apiKeyQuery))
        {
            apiKey = apiKeyQuery.ToString();
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            var result = await apiKeyService.ValidateApiKeyAsync(apiKey);
            if (result.Success && !string.IsNullOrEmpty(result.UserId))
            {
                // Check subscription limit before allowing request
                var hasLimit = await _subscriptionService.CheckRequestLimitAsync(result.UserId);
                if (!hasLimit)
                {
                    context.Response.StatusCode = 429; // Too Many Requests
                    await context.Response.WriteAsJsonAsync(new { 
                        error = "Monthly request limit exceeded. Please upgrade your subscription plan." 
                    });
                    return;
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.UserId ?? ""),
                    new Claim("UserId", result.UserId ?? ""),
                    new Claim("ApiKeyId", result.ApiKeyId ?? "")
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                context.Items["UserId"] = result.UserId;
                context.Items["ApiKeyId"] = result.ApiKeyId;
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API key" });
                return;
            }
        }
        else
        {
            // Generate temporary user ID for external users (as per requirement)
            var tempUserId = Guid.NewGuid().ToString();
            context.Items["UserId"] = tempUserId;
            context.Items["IsTemporaryUser"] = true;

            _logger.LogInformation("Request from temporary user: {UserId}", tempUserId);
        }

        await _next(context);
    }
}

