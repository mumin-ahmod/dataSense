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
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IRolePermissionRepository permissionRepository,
        IMenuRepository menuRepository,
        UserManager<IdentityUser> userManager,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _menuRepository = menuRepository;
        _userManager = userManager;
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
            return new List<MenuPermissionDto>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return new List<MenuPermissionDto>();
        }

        // Get all active menus
        var menus = await _menuRepository.GetActiveMenusAsync();
        
        // Get all permissions for user's roles
        var allPermissions = new List<RolePermission>();
        foreach (var role in roles)
        {
            var rolePermissions = await _permissionRepository.GetByRoleIdAsync(role);
            allPermissions.AddRange(rolePermissions);
        }

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

