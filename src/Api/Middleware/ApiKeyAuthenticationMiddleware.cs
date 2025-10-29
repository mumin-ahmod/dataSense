using System.Security.Claims;
using DataSenseAPI.Application.Abstractions;

namespace DataSenseAPI.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
            string? userId;
            string? apiKeyId;
            if (await apiKeyService.ValidateApiKeyAsync(apiKey, out userId, out apiKeyId))
            {
                // Set user claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId ?? ""),
                    new Claim("UserId", userId ?? ""),
                    new Claim("ApiKeyId", apiKeyId ?? "")
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                // Store in items for easy access
                context.Items["UserId"] = userId;
                context.Items["ApiKeyId"] = apiKeyId;
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

