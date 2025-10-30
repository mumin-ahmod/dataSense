using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;
using System.Security.Claims;

namespace DataSenseAPI.Controllers;

/// <summary>
/// Menu management controller (SystemAdmin only)
/// </summary>
[ApiController]
[Route("api/v1/menu")]
[Authorize(Policy = "SystemAdminOnly")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(IMenuService menuService, ILogger<MenuController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    /// <summary>
    /// Get all menus
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Menu>>> GetAllMenus()
    {
        var menus = await _menuService.GetAllAsync();
        return Ok(menus);
    }

    /// <summary>
    /// Get menu by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Menu>> GetMenu(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        
        if (menu == null)
        {
            return NotFound(new { error = "Menu not found" });
        }

        return Ok(menu);
    }

    /// <summary>
    /// Create a new menu
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Menu>> CreateMenu([FromBody] CreateMenuRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        
        var menu = new Menu
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Icon = request.Icon,
            Url = request.Url,
            ParentId = request.ParentId,
            Order = request.Order,
            IsActive = true
        };

        var created = await _menuService.CreateAsync(menu, userId);
        
        _logger.LogInformation("Menu created: {MenuName} (ID: {MenuId})", created.Name, created.Id);
        
        return CreatedAtAction(nameof(GetMenu), new { id = created.Id }, created);
    }

    /// <summary>
    /// Update an existing menu
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Menu>> UpdateMenu(int id, [FromBody] UpdateMenuRequest request)
    {
        var existing = await _menuService.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { error = "Menu not found" });
        }

        existing.Name = request.Name;
        existing.DisplayName = request.DisplayName;
        existing.Description = request.Description;
        existing.Icon = request.Icon;
        existing.Url = request.Url;
        existing.ParentId = request.ParentId;
        existing.Order = request.Order;
        existing.IsActive = request.IsActive;

        var result = await _menuService.UpdateAsync(existing);
        
        if (!result)
        {
            return BadRequest(new { error = "Failed to update menu" });
        }

        _logger.LogInformation("Menu updated: {MenuName} (ID: {MenuId})", existing.Name, existing.Id);

        return Ok(existing);
    }

    /// <summary>
    /// Delete a menu
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMenu(int id)
    {
        var result = await _menuService.DeleteAsync(id);
        
        if (!result)
        {
            return NotFound(new { error = "Menu not found" });
        }

        _logger.LogInformation("Menu deleted: ID {MenuId}", id);

        return NoContent();
    }
}

public class CreateMenuRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
}

public class UpdateMenuRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Url { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}

