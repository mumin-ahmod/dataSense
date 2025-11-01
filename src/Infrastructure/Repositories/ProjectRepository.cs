using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProjectRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Project?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT project_id as Id, user_id as UserId, name as Name, description as Description,
                   message_channel as MessageChannel, channel_number as ChannelNumber, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   project_key_hash as ProjectKeyHash
            FROM projects
            WHERE project_id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { Id = id });
    }

    public async Task<Project?> GetByKeyHashAsync(string keyHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT project_id as Id, user_id as UserId, name as Name, description as Description,
                   message_channel as MessageChannel, channel_number as ChannelNumber, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   project_key_hash as ProjectKeyHash
            FROM projects
            WHERE project_key_hash = @KeyHash";
        
        return await connection.QueryFirstOrDefaultAsync<Project>(sql, new { KeyHash = keyHash });
    }

    public async Task<List<Project>> GetByUserIdAsync(string userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT project_id as Id, user_id as UserId, name as Name, description as Description,
                   message_channel as MessageChannel, channel_number as ChannelNumber, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   project_key_hash as ProjectKeyHash
            FROM projects
            WHERE user_id = @UserId
            ORDER BY created_at DESC";
        
        var result = await connection.QueryAsync<Project>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<Project>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT project_id as Id, user_id as UserId, name as Name, description as Description,
                   message_channel as MessageChannel, channel_number as ChannelNumber, 
                   is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
                   project_key_hash as ProjectKeyHash
            FROM projects
            ORDER BY created_at DESC";
        
        var result = await connection.QueryAsync<Project>(sql);
        return result.ToList();
    }

    public async Task<Project> CreateAsync(Project project)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO projects (project_id, user_id, name, description, message_channel, channel_number, is_active, created_at, project_key_hash)
            VALUES (@Id, @UserId, @Name, @Description, @MessageChannel, @ChannelNumber, @IsActive, @CreatedAt, @ProjectKeyHash)
            RETURNING project_id";
        
        await connection.ExecuteAsync(sql, new
        {
            Id = project.Id,
            project.UserId,
            project.Name,
            project.Description,
            project.MessageChannel,
            project.ChannelNumber,
            project.IsActive,
            CreatedAt = DateTime.UtcNow,
            project.ProjectKeyHash
        });
        
        return project;
    }

    public async Task<bool> UpdateAsync(Project project)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE projects
            SET name = @Name,
                description = @Description,
                message_channel = @MessageChannel,
                channel_number = @ChannelNumber,
                updated_at = @UpdatedAt
            WHERE project_id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = project.Id,
            project.Name,
            project.Description,
            project.MessageChannel,
            project.ChannelNumber,
            UpdatedAt = DateTime.UtcNow
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> ToggleActiveAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE projects
            SET is_active = NOT is_active,
                updated_at = @UpdatedAt
            WHERE project_id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = id,
            UpdatedAt = DateTime.UtcNow
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = "DELETE FROM projects WHERE project_id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<string> GetProjectKeyByProjectIdAsync(string projectId)
    {
        // This method is not directly available since we store hash, not the key itself
        // This would need to be handled by the service layer that generates keys
        // For now, returning empty string as we don't store the plain key
        await Task.CompletedTask;
        return string.Empty;
    }

    public async Task<bool> UpdateProjectKeyAsync(string projectId, string keyHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            UPDATE projects
            SET project_key_hash = @KeyHash,
                updated_at = @UpdatedAt
            WHERE project_id = @Id";
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = projectId,
            KeyHash = keyHash,
            UpdatedAt = DateTime.UtcNow
        });
        
        return rowsAffected > 0;
    }
}
