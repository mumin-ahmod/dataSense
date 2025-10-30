using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly IRolePermissionRepository _permissionRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IRolePermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<List<RolePermission>> GetAllPermissionsAsync()
    {
        return await _permissionRepository.GetAllAsync();
    }

    public async Task<List<RolePermission>> GetPermissionsByRoleAsync(string roleId)
    {
        return await _permissionRepository.GetByRoleIdAsync(roleId);
    }

    public async Task<List<MenuPermissionDto>> GetMenuPermissionsForUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
            return new List<MenuPermissionDto>();
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            _logger.LogWarning("User {UserId} has no roles assigned", userId);
            return new List<MenuPermissionDto>();
        }

        _logger.LogInformation("User {UserId} has roles: {Roles}", userId, string.Join(", ", roleNames));

        // Convert role names to role IDs
        var roleIds = new List<string>();
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                roleIds.Add(role.Id);
                _logger.LogInformation("Resolved role '{RoleName}' to ID: {RoleId}", roleName, role.Id);
            }
            else
            {
                _logger.LogWarning("Role '{RoleName}' not found in AspNetRoles", roleName);
            }
        }

        if (roleIds.Count == 0)
        {
            _logger.LogWarning("Could not resolve any role IDs for user {UserId}", userId);
            return new List<MenuPermissionDto>();
        }

        // Get all active menus
        var menus = await _menuRepository.GetActiveMenusAsync();
        _logger.LogInformation("Found {Count} active menus", menus.Count);
        
        // Get all permissions for user's roles
        var allPermissions = new List<RolePermission>();
        foreach (var roleId in roleIds)
        {
            var rolePermissions = await _permissionRepository.GetByRoleIdAsync(roleId);
            _logger.LogInformation("Role {RoleId} has {Count} permissions", roleId, rolePermissions.Count);
            allPermissions.AddRange(rolePermissions);
        }

        _logger.LogInformation("Total permissions fetched: {Count}", allPermissions.Count);

        // Group by menu and aggregate permissions (any role with permission = user has permission)
        var menuPermissions = menus.Select(menu =>
        {
            var perms = allPermissions.Where(p => p.MenuId == menu.Id).ToList();
            return new MenuPermissionDto
            {
                MenuId = menu.Id,
                MenuName = menu.Name,
                DisplayName = menu.DisplayName,
                Icon = menu.Icon,
                Url = menu.Url,
                ParentId = menu.ParentId,
                Order = menu.Order,
                CanView = perms.Any(p => p.CanView),
                CanCreate = perms.Any(p => p.CanCreate),
                CanEdit = perms.Any(p => p.CanEdit),
                CanDelete = perms.Any(p => p.CanDelete)
            };
        })
        .Where(mp => mp.CanView) // Only return menus the user can view
        .OrderBy(mp => mp.Order)
        .ToList();

        _logger.LogInformation("Returning {Count} menu permissions with view access", menuPermissions.Count);

        return menuPermissions;
    }

    public async Task<bool> SetRolePermissionAsync(RolePermission permission, string createdBy)
    {
        try
        {
            // Check if permission already exists
            var existing = await _permissionRepository.GetByRoleAndMenuAsync(permission.RoleId, permission.MenuId);
            
            if (existing != null)
            {
                // Update existing permission
                existing.CanView = permission.CanView;
                existing.CanCreate = permission.CanCreate;
                existing.CanEdit = permission.CanEdit;
                existing.CanDelete = permission.CanDelete;
                
                return await _permissionRepository.UpdateAsync(existing);
            }
            else
            {
                // Create new permission
                permission.CreatedBy = createdBy;
                permission.CreatedAt = DateTime.UtcNow;
                await _permissionRepository.CreateAsync(permission);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting role permission for Role {RoleId}, Menu {MenuId}", 
                permission.RoleId, permission.MenuId);
            return false;
        }
    }

    public async Task<bool> SetRolePermissionsBulkAsync(string roleId, List<RolePermission> permissions, string createdBy)
    {
        try
        {
            // Delete all existing permissions for this role
            await _permissionRepository.DeleteByRoleIdAsync(roleId);
            
            // Create new permissions
            foreach (var permission in permissions)
            {
                permission.RoleId = roleId;
                permission.CreatedBy = createdBy;
                permission.CreatedAt = DateTime.UtcNow;
                await _permissionRepository.CreateAsync(permission);
            }
            
            _logger.LogInformation("Bulk role permissions set for Role {RoleId}: {Count} permissions", 
                roleId, permissions.Count);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting bulk role permissions for Role {RoleId}", roleId);
            return false;
        }
    }
}

