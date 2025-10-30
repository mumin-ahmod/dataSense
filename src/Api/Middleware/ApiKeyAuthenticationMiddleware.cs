using System.Security.Claims;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IProjectService _projectService;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next, 
        ILogger<ApiKeyAuthenticationMiddleware> logger,
        ISubscriptionService subscriptionService,
        IProjectService projectService)
    {
        _next = next;
        _logger = logger;
        _subscriptionService = subscriptionService;
        _projectService = projectService;
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
        string? projectKey = null;

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
            else if (authValue.StartsWith("ProjectKey ", StringComparison.OrdinalIgnoreCase))
            {
                projectKey = authValue.Substring("ProjectKey ".Length).Trim();
            }
        }

        // Try to get from query string as fallback
        if (string.IsNullOrEmpty(apiKey) && context.Request.Query.TryGetValue("apiKey", out var apiKeyQuery))
        {
            apiKey = apiKeyQuery.ToString();
        }

        // Try to get ProjectKey from headers
        if (string.IsNullOrEmpty(projectKey))
        {
            if (context.Request.Headers.TryGetValue("X-Project-Key", out var pkHeader))
            {
                projectKey = pkHeader.ToString();
            }
            else if (context.Request.Headers.TryGetValue("ProjectKey", out var pkHeader2))
            {
                projectKey = pkHeader2.ToString();
            }
        }

        // Try to get from query string as fallback
        if (string.IsNullOrEmpty(projectKey) && context.Request.Query.TryGetValue("projectKey", out var projectKeyQuery))
        {
            projectKey = projectKeyQuery.ToString();
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

                // If a ProjectKey was provided, validate and attach ProjectId
                if (!string.IsNullOrEmpty(projectKey))
                {
                    var projectValidation = await _projectService.ValidateProjectKeyAsync(projectKey);
                    if (!projectValidation.Success || string.IsNullOrEmpty(projectValidation.ProjectId))
                    {
                        context.Response.StatusCode = 401; // Unauthorized
                        await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired Project key" });
                        return;
                    }

                    // Ensure the project belongs to the same user as the API key
                    if (!string.Equals(projectValidation.UserId, result.UserId, StringComparison.Ordinal))
                    {
                        context.Response.StatusCode = 403; // Forbidden
                        await context.Response.WriteAsJsonAsync(new { error = "Project does not belong to this API key owner" });
                        return;
                    }

                    context.Items["ProjectId"] = projectValidation.ProjectId;
                }
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

