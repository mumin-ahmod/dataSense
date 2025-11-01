using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DataSenseAPI.Application.Commands.SubscriptionPlan;
using DataSenseAPI.Application.Queries.SubscriptionPlan;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Subscription plan management controller
/// </summary>
[ApiController]
[Route("api/v1/subscription-plans")]
public class SubscriptionPlanController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SubscriptionPlanController> _logger;
    private readonly IApiKeyRepository _apiKeyRepository;

    public SubscriptionPlanController(
        IMediator mediator, 
        ILogger<SubscriptionPlanController> logger,
        IApiKeyRepository apiKeyRepository)
    {
        _mediator = mediator;
        _logger = logger;
        _apiKeyRepository = apiKeyRepository;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirst("UserId")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    /// <summary>
    /// Get all active subscription plans (Public API - no authentication required)
    /// </summary>
    [HttpGet("public/active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<SubscriptionPlanResponse>>> GetActiveSubscriptionPlans()
    {
        try
        {
            var plans = await _mediator.Send(new GetActiveSubscriptionPlansQuery());
            
            var response = plans.Select(p => new SubscriptionPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                MonthlyRequestLimit = p.MonthlyRequestLimit,
                MonthlyPrice = p.MonthlyPrice,
                AbroadMonthlyPrice = p.AbroadMonthlyPrice,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                Features = p.Features
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription plans");
            return StatusCode(500, new { error = "Failed to get subscription plans", message = ex.Message });
        }
    }

    /// <summary>
    /// Get all subscription plans (SystemAdmin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "SystemAdmin")]
    public async Task<ActionResult<List<SubscriptionPlanResponse>>> GetAllSubscriptionPlans()
    {
        try
        {
            var plans = await _mediator.Send(new GetAllSubscriptionPlansQuery());
            
            var response = plans.Select(p => new SubscriptionPlanResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                MonthlyRequestLimit = p.MonthlyRequestLimit,
                MonthlyPrice = p.MonthlyPrice,
                AbroadMonthlyPrice = p.AbroadMonthlyPrice,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Features = p.Features
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return StatusCode(500, new { error = "Failed to get subscription plans", message = ex.Message });
        }
    }

    /// <summary>
    /// Get subscription plan by ID (SystemAdmin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "SystemAdmin")]
    public async Task<ActionResult<SubscriptionPlanResponse>> GetSubscriptionPlan(string id)
    {
        try
        {
            var plan = await _mediator.Send(new GetSubscriptionPlanByIdQuery(id));
            
            if (plan == null)
            {
                return NotFound(new { error = "Subscription plan not found" });
            }

            var response = new SubscriptionPlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                MonthlyRequestLimit = plan.MonthlyRequestLimit,
                MonthlyPrice = plan.MonthlyPrice,
                AbroadMonthlyPrice = plan.AbroadMonthlyPrice,
                IsActive = plan.IsActive,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt,
                Features = plan.Features
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan {PlanId}", id);
            return StatusCode(500, new { error = "Failed to get subscription plan", message = ex.Message });
        }
    }

    /// <summary>
    /// Create a new subscription plan (SystemAdmin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SystemAdmin")]
    public async Task<ActionResult<CreateSubscriptionPlanResponse>> CreateSubscriptionPlan([FromBody] CreateSubscriptionPlanRequest request)
    {
        try
        {
            var command = new CreateSubscriptionPlanCommand(
                request.Name,
                request.Description,
                request.MonthlyRequestLimit,
                request.MonthlyPrice,
                request.AbroadMonthlyPrice,
                request.IsActive,
                request.Features
            );

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetSubscriptionPlan), new { id = result.PlanId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription plan");
            return StatusCode(500, new { error = "Failed to create subscription plan", message = ex.Message });
        }
    }

    /// <summary>
    /// Update a subscription plan (SystemAdmin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> UpdateSubscriptionPlan(string id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        try
        {
            var command = new UpdateSubscriptionPlanCommand(
                id,
                request.Name,
                request.Description,
                request.MonthlyRequestLimit,
                request.MonthlyPrice,
                request.AbroadMonthlyPrice,
                request.IsActive,
                request.Features
            );

            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { error = "Subscription plan not found" });
            }

            return Ok(new { success = true, message = "Subscription plan updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription plan {PlanId}", id);
            return StatusCode(500, new { error = "Failed to update subscription plan", message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a subscription plan (soft delete - SystemAdmin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "SystemAdmin")]
    public async Task<IActionResult> DeleteSubscriptionPlan(string id)
    {
        try
        {
            var command = new DeleteSubscriptionPlanCommand(id);
            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return NotFound(new { error = "Subscription plan not found" });
            }

            return Ok(new { success = true, message = "Subscription plan deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription plan {PlanId}", id);
            return StatusCode(500, new { error = "Failed to delete subscription plan", message = ex.Message });
        }
    }

    /// <summary>
    /// Buy a subscription plan (initiate payment)
    /// </summary>
    [HttpPost("{planId}/buy")]
    [Authorize]
    public async Task<ActionResult<BuySubscriptionResponse>> BuySubscription(string planId, [FromBody] BuySubscriptionRequest request)
    {
        try
        {
            var userId = GetUserId();
            var command = new BuySubscriptionCommand(
                userId,
                planId,
                request.PaymentProvider,
                request.IsAbroad
            );

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error buying subscription for plan {PlanId}", planId);
            return StatusCode(500, new { error = "Failed to buy subscription", message = ex.Message });
        }
    }

    /// <summary>
    /// Process payment after successful payment
    /// </summary>
    [HttpPost("payments/{paymentId}/process")]
    [Authorize]
    public async Task<IActionResult> ProcessPayment(string paymentId, [FromBody] ProcessPaymentRequest request)
    {
        try
        {
            var command = new ProcessPaymentCommand(
                paymentId,
                request.TransactionId,
                request.PaymentProvider
            );

            var result = await _mediator.Send(command);
            
            if (!result)
            {
                return BadRequest(new { error = "Failed to process payment" });
            }

            return Ok(new { success = true, message = "Payment processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "Failed to process payment", message = ex.Message });
        }
    }

    /// <summary>
    /// Get all API keys for a user. Requires userId parameter. 
    /// Owner can get their own API keys. SystemAdmin can get any user's API keys.
    /// </summary>
    [HttpGet("api-keys")]
    [Authorize]
    public async Task<ActionResult<List<ApiKeyResponse>>> GetApiKeys([FromQuery] string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new { error = "userId parameter is required" });
            }

            // Get current user ID from JWT claims
            var currentUserId = GetUserId();
            var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var isSystemAdmin = currentUserRoles.Contains("SystemAdmin");
            
            // Verify ownership or SystemAdmin role
            if (!isSystemAdmin && currentUserId != userId)
            {
                return StatusCode(403, new { error = "Forbidden", message = "You can only retrieve your own API keys" });
            }
            
            // Get API keys for the requested user (only one active API key per user)
            var apiKeys = await _apiKeyRepository.GetByUserIdAsync(userId);
            
            // Filter to only return active API keys (there should be only one)
            var activeApiKeys = apiKeys.Where(k => k.IsActive).ToList();
            
            var response = activeApiKeys.Select(k => new ApiKeyResponse
            {
                KeyId = k.Id,
                Name = k.Name,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.ExpiresAt, // Note: ExpiresAt actually maps to last_used_at in DB
                UserMetadata = k.UserMetadata
            }).ToList();

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return StatusCode(500, new { error = "Failed to retrieve API keys", message = ex.Message });
        }
    }

    /// <summary>
    /// Generate API key for the current user
    /// </summary>
    [HttpPost("api-keys/generate")]
    [Authorize]
    public async Task<ActionResult<GenerateApiKeyResponse>> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
    {
        try
        {
            var userId = GetUserId();
            var command = new GenerateApiKeyCommand(
                userId,
                request.Name,
                request.Metadata
            );

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key");
            return StatusCode(500, new { error = "Failed to generate API key", message = ex.Message });
        }
    }
}

// Request DTOs
public class CreateSubscriptionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? AbroadMonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object>? Features { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? AbroadMonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object>? Features { get; set; }
}

// Request DTOs for new endpoints
public class BuySubscriptionRequest
{
    public string? PaymentProvider { get; set; }
    public bool IsAbroad { get; set; } = false;
}

public class ProcessPaymentRequest
{
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentProvider { get; set; } = string.Empty;
}

public class GenerateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ApiKeyResponse
{
    public string KeyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public Dictionary<string, object>? UserMetadata { get; set; }
}

// Response DTOs
public class SubscriptionPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MonthlyRequestLimit { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal? AbroadMonthlyPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, object>? Features { get; set; }
}

