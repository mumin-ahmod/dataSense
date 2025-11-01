using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Permission management controller (SystemAdmin only)
/// </summary>
[ApiController]
[Route("api/v1/permission")]
[Authorize(Policy = "SystemAdmin")]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly IRolePermissionRepository _permissionRepository;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(
        IPermissionService permissionService,
        IRolePermissionRepository permissionRepository,
        RoleManager<IdentityRole> roleManager,
        ILogger<PermissionController> logger)
    {
        _permissionService = permissionService;
        _permissionRepository = permissionRepository;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all role permissions
    /// </summary>
    [HttpGet("role-permissions")]
    public async Task<ActionResult<List<RolePermissionDto>>> GetRolePermissions()
    {
        var permissions = await _permissionService.GetAllPermissionsAsync();
        
        var result = new List<RolePermissionDto>();
        foreach (var perm in permissions)
        {
            var role = await _roleManager.FindByIdAsync(perm.RoleId);
            result.Add(new RolePermissionDto
            {
                Id = perm.Id,
                RoleId = perm.RoleId,
                RoleName = role?.Name ?? "Unknown",
                MenuId = perm.MenuId,
                MenuName = "", // Could be enhanced by joining with menu
                CanView = perm.CanView,
                CanCreate = perm.CanCreate,
                CanEdit = perm.CanEdit,
                CanDelete = perm.CanDelete
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Get role permissions by role ID
    /// </summary>
    [HttpGet("role-permissions/{roleId}")]
    public async Task<ActionResult<List<RolePermissionDto>>> GetRolePermissionsByRole(string roleId)
    {
        var permissions = await _permissionService.GetPermissionsByRoleAsync(roleId);
        var role = await _roleManager.FindByIdAsync(roleId);

        var result = permissions.Select(perm => new RolePermissionDto
        {
            Id = perm.Id,
            RoleId = perm.RoleId,
            RoleName = role?.Name ?? "Unknown",
            MenuId = perm.MenuId,
            MenuName = "", // Could be enhanced
            CanView = perm.CanView,
            CanCreate = perm.CanCreate,
            CanEdit = perm.CanEdit,
            CanDelete = perm.CanDelete
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Set role permission for a specific menu
    /// </summary>
    [HttpPost("role-permission")]
    public async Task<IActionResult> SetRolePermission([FromBody] SetRolePermissionRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        
        var permission = new RolePermission
        {
            RoleId = request.RoleId,
            MenuId = request.MenuId,
            CanView = request.CanView,
            CanCreate = request.CanCreate,
            CanEdit = request.CanEdit,
            CanDelete = request.CanDelete
        };

        var result = await _permissionService.SetRolePermissionAsync(permission, userId);

        if (!result)
        {
            return BadRequest(new { error = "Failed to set role permission" });
        }

        _logger.LogInformation("Role permission set: RoleId={RoleId}, MenuId={MenuId}", request.RoleId, request.MenuId);

        return Ok(new { message = "Role permission set successfully" });
    }

    /// <summary>
    /// Set multiple role permissions at once
    /// </summary>
    [HttpPost("role-permission-bulk")]
    public async Task<IActionResult> SetRolePermissionBulk([FromBody] SetRolePermissionBulkRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        
        var permissions = request.MenuPermissions.Select(mp => new RolePermission
        {
            RoleId = request.RoleId,
            MenuId = mp.MenuId,
            CanView = mp.CanView,
            CanCreate = mp.CanCreate,
            CanEdit = mp.CanEdit,
            CanDelete = mp.CanDelete
        }).ToList();

        var result = await _permissionService.SetRolePermissionsBulkAsync(request.RoleId, permissions, userId);

        if (!result)
        {
            return BadRequest(new { error = "Failed to set bulk role permissions" });
        }

        _logger.LogInformation("Bulk role permissions set: RoleId={RoleId}, Count={Count}", 
            request.RoleId, permissions.Count);

        return Ok(new { message = "Bulk role permissions set successfully" });
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles")]
    public ActionResult<List<RoleDto>> GetRoles()
    {
        var roles = _roleManager.Roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name ?? ""
        }).ToList();

        return Ok(roles);
    }
}

public class RolePermissionDto
{
    public int Id { get; set; }
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int MenuId { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class SetRolePermissionRequest
{
    public string RoleId { get; set; } = string.Empty;
    public int MenuId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class SetRolePermissionBulkRequest
{
    public string RoleId { get; set; } = string.Empty;
    public List<MenuPermissionRequest> MenuPermissions { get; set; } = new();
}

public class MenuPermissionRequest
{
    public int MenuId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

