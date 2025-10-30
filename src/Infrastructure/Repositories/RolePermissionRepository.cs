using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RolePermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RolePermission?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, role_id as RoleId, menu_id as MenuId,
                   can_view as CanView, can_create as CanCreate, can_edit as CanEdit, can_delete as CanDelete,
                   created_at as CreatedAt, created_by as CreatedBy
            FROM role_permissions
            WHERE id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<RolePermission>(sql, new { Id = id });
    }

    public async Task<List<RolePermission>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, role_id as RoleId, menu_id as MenuId,
                   can_view as CanView, can_create as CanCreate, can_edit as CanEdit, can_delete as CanDelete,
                   created_at as CreatedAt, created_by as CreatedBy
            FROM role_permissions";
        
        var result = await connection.QueryAsync<RolePermission>(sql);
        return result.ToList();
    }

    public async Task<List<RolePermission>> GetByRoleIdAsync(string roleId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, role_id as RoleId, menu_id as MenuId,
                   can_view as CanView, can_create as CanCreate, can_edit as CanEdit, can_delete as CanDelete,
                   created_at as CreatedAt, created_by as CreatedBy
            FROM role_permissions
            WHERE role_id = @RoleId";
        
        var result = await connection.QueryAsync<RolePermission>(sql, new { RoleId = roleId });
        return result.ToList();
    }

    public async Task<List<RolePermission>> GetByMenuIdAsync(int menuId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, role_id as RoleId, menu_id as MenuId,
                   can_view as CanView, can_create as CanCreate, can_edit as CanEdit, can_delete as CanDelete,
                   created_at as CreatedAt, created_by as CreatedBy
            FROM role_permissions
            WHERE menu_id = @MenuId";
        
        var result = await connection.QueryAsync<RolePermission>(sql, new { MenuId = menuId });
        return result.ToList();
    }

    public async Task<RolePermission?> GetByRoleAndMenuAsync(string roleId, int menuId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, role_id as RoleId, menu_id as MenuId,
                   can_view as CanView, can_create as CanCreate, can_edit as CanEdit, can_delete as CanDelete,
                   created_at as CreatedAt, created_by as CreatedBy
            FROM role_permissions
            WHERE role_id = @RoleId AND menu_id = @MenuId";
        
        return await connection.QueryFirstOrDefaultAsync<RolePermission>(sql, new { RoleId = roleId, MenuId = menuId });
    }

    public async Task<RolePermission> CreateAsync(RolePermission permission)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO role_permissions (role_id, menu_id, can_view, can_create, can_edit, can_delete, created_at, created_by)
            VALUES (@RoleId, @MenuId, @CanView, @CanCreate, @CanEdit, @CanDelete, @CreatedAt, @CreatedBy)
            RETURNING id";
        
        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            permission.RoleId,
            permission.MenuId,
            permission.CanView,
            permission.CanCreate,
            permission.CanEdit,
            permission.CanDelete,
            CreatedAt = DateTime.UtcNow,
            permission.CreatedBy
        });
        
        permission.Id = id;
        return permission;
    }

    public async Task<bool> UpdateAsync(RolePermission permission)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE role_permissions
            SET can_view = @CanView,
                can_create = @CanCreate,
                can_edit = @CanEdit,
                can_delete = @CanDelete
            WHERE id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            permission.Id,
            permission.CanView,
            permission.CanCreate,
            permission.CanEdit,
            permission.CanDelete
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM role_permissions WHERE id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByRoleIdAsync(string roleId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM role_permissions WHERE role_id = @RoleId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { RoleId = roleId });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByMenuIdAsync(int menuId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM role_permissions WHERE menu_id = @MenuId";
        var rowsAffected = await connection.ExecuteAsync(sql, new { MenuId = menuId });
        return rowsAffected > 0;
    }
}

