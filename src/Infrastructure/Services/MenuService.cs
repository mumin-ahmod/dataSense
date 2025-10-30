using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataSenseAPI.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuService> _logger;

    public MenuService(IMenuRepository menuRepository, ILogger<MenuService> logger)
    {
        _menuRepository = menuRepository;
        _logger = logger;
    }

    public async Task<Menu?> GetByIdAsync(int id)
    {
        return await _menuRepository.GetByIdAsync(id);
    }

    public async Task<List<Menu>> GetAllAsync()
    {
        return await _menuRepository.GetAllAsync();
    }

    public async Task<List<Menu>> GetActiveMenusAsync()
    {
        return await _menuRepository.GetActiveMenusAsync();
    }

    public async Task<Menu> CreateAsync(Menu menu, string createdBy)
    {
        menu.CreatedBy = createdBy;
        menu.CreatedAt = DateTime.UtcNow;
        menu.UpdatedAt = null;
        
        var created = await _menuRepository.CreateAsync(menu);
        _logger.LogInformation("Menu created: {MenuName} (ID: {MenuId}) by {CreatedBy}", 
            created.Name, created.Id, createdBy);
        
        return created;
    }

    public async Task<bool> UpdateAsync(Menu menu)
    {
        menu.UpdatedAt = DateTime.UtcNow;
        var result = await _menuRepository.UpdateAsync(menu);
        
        if (result)
        {
            _logger.LogInformation("Menu updated: {MenuName} (ID: {MenuId})", menu.Name, menu.Id);
        }
        
        return result;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _menuRepository.DeleteAsync(id);
        
        if (result)
        {
            _logger.LogInformation("Menu deleted: ID {MenuId}", id);
        }
        
        return result;
    }
}

