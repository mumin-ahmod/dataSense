using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MenuRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Menu?> GetByIdAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, name as Name, display_name as DisplayName, description as Description,
                   icon as Icon, url as Url, parent_id as ParentId, order_index as ""Order"",
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   created_by as CreatedBy
            FROM menus
            WHERE id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<Menu>(sql, new { Id = id });
    }

    public async Task<List<Menu>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, name as Name, display_name as DisplayName, description as Description,
                   icon as Icon, url as Url, parent_id as ParentId, order_index as ""Order"",
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   created_by as CreatedBy
            FROM menus
            ORDER BY order_index, id";
        
        var result = await connection.QueryAsync<Menu>(sql);
        return result.ToList();
    }

    public async Task<List<Menu>> GetActiveMenusAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT id as Id, name as Name, display_name as DisplayName, description as Description,
                   icon as Icon, url as Url, parent_id as ParentId, order_index as ""Order"",
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   created_by as CreatedBy
            FROM menus
            WHERE is_active = true
            ORDER BY order_index, id";
        
        var result = await connection.QueryAsync<Menu>(sql);
        return result.ToList();
    }

    public async Task<Menu> CreateAsync(Menu menu)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO menus (name, display_name, description, icon, url, parent_id, order_index, is_active, created_at, created_by)
            VALUES (@Name, @DisplayName, @Description, @Icon, @Url, @ParentId, @Order, @IsActive, @CreatedAt, @CreatedBy)
            RETURNING id";
        
        var id = await connection.ExecuteScalarAsync<int>(sql, new
        {
            menu.Name,
            menu.DisplayName,
            menu.Description,
            menu.Icon,
            menu.Url,
            menu.ParentId,
            Order = menu.Order,
            menu.IsActive,
            CreatedAt = DateTime.UtcNow,
            menu.CreatedBy
        });
        
        menu.Id = id;
        return menu;
    }

    public async Task<bool> UpdateAsync(Menu menu)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE menus
            SET name = @Name,
                display_name = @DisplayName,
                description = @Description,
                icon = @Icon,
                url = @Url,
                parent_id = @ParentId,
                order_index = @Order,
                is_active = @IsActive,
                updated_at = @UpdatedAt
            WHERE id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            menu.Id,
            menu.Name,
            menu.DisplayName,
            menu.Description,
            menu.Icon,
            menu.Url,
            menu.ParentId,
            Order = menu.Order,
            menu.IsActive,
            UpdatedAt = DateTime.UtcNow
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM menus WHERE id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<Menu>> GetByParentIdAsync(int? parentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        string sql;
        if (parentId == null)
        {
            sql = @"
                SELECT id as Id, name as Name, display_name as DisplayName, description as Description,
                       icon as Icon, url as Url, parent_id as ParentId, order_index as ""Order"",
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                       created_by as CreatedBy
                FROM menus
                WHERE parent_id IS NULL
                ORDER BY order_index, id";
            var result = await connection.QueryAsync<Menu>(sql);
            return result.ToList();
        }
        else
        {
            sql = @"
                SELECT id as Id, name as Name, display_name as DisplayName, description as Description,
                       icon as Icon, url as Url, parent_id as ParentId, order_index as ""Order"",
                       is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                       created_by as CreatedBy
                FROM menus
                WHERE parent_id = @ParentId
                ORDER BY order_index, id";
            var result = await connection.QueryAsync<Menu>(sql, new { ParentId = parentId });
            return result.ToList();
        }
    }
}

