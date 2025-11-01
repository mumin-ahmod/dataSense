using Dapper;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Repositories;

public class MessageChannelRepository : IMessageChannelRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MessageChannelRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MessageChannel?> GetByIdAsync(string id)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT channel_id::text as Id, name as Name, description as Description, created_at as CreatedAt
            FROM message_channels
            WHERE channel_id = @Id::uuid";
        
        return await connection.QueryFirstOrDefaultAsync<MessageChannel>(sql, new { Id = id });
    }

    public async Task<MessageChannel?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT channel_id as Id, name as Name, description as Description, created_at as CreatedAt
            FROM message_channels
            WHERE name = @Name";
        
        return await connection.QueryFirstOrDefaultAsync<MessageChannel>(sql, new { Name = name });
    }

    public async Task<List<MessageChannel>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT channel_id::text as Id, name as Name, description as Description, created_at as CreatedAt
            FROM message_channels
            ORDER BY name ASC";
        
        var result = await connection.QueryAsync<MessageChannel>(sql);
        return result.ToList();
    }
}

