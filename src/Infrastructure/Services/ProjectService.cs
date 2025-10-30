using System.Security.Cryptography;
using System.Text;
using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using DataSenseAPI.Infrastructure.Repositories;

namespace DataSenseAPI.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IDbConnectionFactory connectionFactory, ILogger<ProjectService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<string> GenerateProjectKeyAsync(string userId, string projectName)
    {
        var keyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        var projectKey = Convert.ToBase64String(keyBytes);
        var hash = HashKey(projectKey);

        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
INSERT INTO projects (project_id, user_id, name, description, message_channel, channel_number, is_active, created_at, project_key_hash)
VALUES (@Id, @UserId, @Name, @Description, @MessageChannel, @ChannelNumber, TRUE, NOW(), @ProjectKeyHash)
ON CONFLICT (project_id) DO NOTHING;";

        var id = Guid.NewGuid().ToString();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            UserId = userId,
            Name = projectName,
            Description = (string?)null,
            MessageChannel = "api",
            ChannelNumber = (string?)null,
            ProjectKeyHash = hash
        });

        _logger.LogInformation("Project created with id {ProjectId} for user {UserId}", id, userId);
        return projectKey;
    }

    public async Task<ProjectValidationResult> ValidateProjectKeyAsync(string projectKey)
    {
        try
        {
            var hash = HashKey(projectKey);
            using var conn = _connectionFactory.CreateConnection();
            const string sql = @"
SELECT project_id as ProjectId, user_id as UserId, is_active as IsActive
FROM projects
WHERE project_key_hash = @Hash
LIMIT 1";
            var row = await conn.QueryFirstOrDefaultAsync<(string ProjectId, string UserId, bool IsActive)>(sql, new { Hash = hash });
            if (row.ProjectId == null || !row.IsActive)
            {
                return new ProjectValidationResult { Success = false };
            }
            return new ProjectValidationResult { Success = true, ProjectId = row.ProjectId, UserId = row.UserId };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Project key validation failed");
            return new ProjectValidationResult { Success = false };
        }
    }

    public async Task<Project?> GetProjectByIdAsync(string projectId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
SELECT project_id as Id, user_id as UserId, name, description, message_channel as MessageChannel,
       channel_number as ChannelNumber, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
       project_key_hash as ProjectKeyHash
FROM projects
WHERE project_id = @Id";
        return await conn.QueryFirstOrDefaultAsync<Project>(sql, new { Id = projectId });
    }

    public async Task<List<Project>> GetProjectsByUserAsync(string userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        const string sql = @"
SELECT project_id as Id, user_id as UserId, name, description, message_channel as MessageChannel,
       channel_number as ChannelNumber, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt,
       project_key_hash as ProjectKeyHash
FROM projects
WHERE user_id = @UserId
ORDER BY created_at DESC";
        var rows = await conn.QueryAsync<Project>(sql, new { UserId = userId });
        return rows.AsList();
    }

    private static string HashKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
