using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using DataSenseAPI.Application.Commands.SubscriptionPlan;
using DataSenseAPI.Application.Queries.SubscriptionPlan;
using DataSenseAPI.Domain.Models;

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

    public SubscriptionPlanController(IMediator mediator, ILogger<SubscriptionPlanController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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

