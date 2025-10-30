using DataSenseAPI.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<UserSearchResult> SearchUsersAsync(string? searchTerm, int page, int pageSize, string? forPage, string? currentUserRoles)
    {
        var query = _userManager.Users.AsQueryable();

        // Filter out SystemAdmin users if the current user is not a SystemAdmin
        var isSystemAdmin = currentUserRoles?.Contains("SystemAdmin") ?? false;
        if (!isSystemAdmin)
        {
            // Get SystemAdmin role ID
            var systemAdminRole = await _roleManager.FindByNameAsync("SystemAdmin");
            if (systemAdminRole != null)
            {
                var systemAdminUserIds = await _userManager.GetUsersInRoleAsync("SystemAdmin");
                var systemAdminIds = systemAdminUserIds.Select(u => u.Id).ToList();
                query = query.Where(u => !systemAdminIds.Contains(u.Id));
            }
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => 
                u.UserName!.Contains(searchTerm) || 
                u.Email!.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = null, // IdentityUser doesn't have FirstName by default
                LastName = null,  // IdentityUser doesn't have LastName by default
                Roles = roles.ToList(),
                IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow,
                CreatedAt = DateTime.UtcNow // IdentityUser doesn't track creation date by default
            });
        }

        return new UserSearchResult
        {
            Users = userDtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    public async Task<UserDetailsDto?> GetUserDetailsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        
        return new UserDetailsDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = null,
            LastName = null,
            Roles = roles.ToList(),
            Permissions = new List<string>(), // Could be extended to list specific permissions
            IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<PublicUserDto?> GetPublicUserInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new PublicUserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = null,
            LastName = null,
            PhoneNumber = user.PhoneNumber
        };
    }

    public async Task<bool> ChangeUserRoleAsync(string userId, string roleId, string performedBy)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning("Role not found: {RoleId}", roleId);
                return false;
            }

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove existing roles for user {UserId}", userId);
                    return false;
                }
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!addResult.Succeeded)
            {
                _logger.LogError("Failed to add role {RoleName} to user {UserId}", role.Name, userId);
                return false;
            }

            _logger.LogInformation("User {UserId} role changed to {RoleName} by {PerformedBy}", 
                userId, role.Name, performedBy);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing user role for {UserId} to {RoleId}", userId, roleId);
            return false;
        }
    }
}

