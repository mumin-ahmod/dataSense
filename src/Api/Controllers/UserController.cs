using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataSenseAPI.Application.Abstractions;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/v1/user")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserManagementService userManagementService, ILogger<UserController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Search users with filtering and pagination
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<UserSearchResult>> SearchUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? forPage = "public")
    {
        var currentUserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var currentUserRolesString = string.Join(",", currentUserRoles);

        var result = await _userManagementService.SearchUsersAsync(searchTerm, page, pageSize, forPage, currentUserRolesString);
        
        return Ok(result);
    }

    /// <summary>
    /// Get detailed user information (SystemAdmin only)
    /// </summary>
    [HttpGet("{userId}")]
    [Authorize(Policy = "SystemAdminOnly")]
    public async Task<ActionResult<UserDetailsDto>> GetUserDetails(string userId)
    {
        var user = await _userManagementService.GetUserDetailsAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Get public user information
    /// </summary>
    [HttpGet("{userId}/public")]
    public async Task<ActionResult<PublicUserDto>> GetPublicUserInfo(string userId)
    {
        var user = await _userManagementService.GetPublicUserInfoAsync(userId);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Change user role (SystemAdmin only)
    /// </summary>
    [HttpPost("change-role")]
    [Authorize(Policy = "SystemAdminOnly")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeRoleRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.RoleId))
        {
            return BadRequest(new { error = "UserId and RoleId are required" });
        }

        var performedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _userManagementService.ChangeUserRoleAsync(request.UserId, request.RoleId, performedBy);

        if (!result)
        {
            return BadRequest(new { success = false, message = "Failed to change user role" });
        }

        _logger.LogInformation("User role changed: UserId={UserId}, RoleId={RoleId}, PerformedBy={PerformedBy}", 
            request.UserId, request.RoleId, performedBy);

        return Ok(new { success = true, message = "User role changed successfully" });
    }
}

public class ChangeRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
}

