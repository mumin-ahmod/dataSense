using Microsoft.AspNetCore.Identity;

namespace DataSenseAPI.Domain.Models;

/// <summary>
/// Custom application user extending IdentityUser with additional properties
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

