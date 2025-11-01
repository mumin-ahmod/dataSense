using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DataSenseAPI.Application.Abstractions;

namespace DataSenseAPI.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly string _issuer;
    private readonly string _audience;
    private const int AccessTokenExpiryMinutes = 15; // Short-lived access token
    private const int RefreshTokenExpiryDays = 7;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "datasense";
        _audience = configuration["Jwt:Audience"] ?? "datasense-api";
    }

    public Task<string> GenerateAccessTokenAsync(string userId, string email, string? userName, IList<string> roles)
    {
        // Claims: user id, email, and roles
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email ?? string.Empty)
        };

        // Add roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(AccessTokenExpiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        return Task.FromResult(tokenString);
    }

    public Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);
        return Task.FromResult(refreshToken);
    }

    public Task<bool> ValidateRefreshTokenAsync(string token)
    {
        // Refresh tokens are validated by checking existence and expiration in database
        // This method can be used for format validation if needed
        return Task.FromResult(!string.IsNullOrWhiteSpace(token) && token.Length > 20);
    }
}

