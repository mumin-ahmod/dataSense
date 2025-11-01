using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _repository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<ApiKeyService> _logger;
    private readonly string _jwtSecret;

    public ApiKeyService(
        IApiKeyRepository repository, 
        ISubscriptionService subscriptionService,
        ILogger<ApiKeyService> logger, 
        IConfiguration configuration)
    {
        _repository = repository;
        _subscriptionService = subscriptionService;
        _logger = logger;
        _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
    }

    public async Task<string> GenerateApiKeyAsync(string userId, string name, Dictionary<string, object>? metadata = null)
    {
        // Generate a random API key
        var apiKeyBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(apiKeyBytes);
        var apiKey = Convert.ToBase64String(apiKeyBytes);

        // Hash the API key for storage
        var keyHash = HashApiKey(apiKey);

        // Generate key ID
        var keyId = Guid.NewGuid().ToString();

        // Create JWT token with claims
        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim("keyId", keyId),
            new Claim("name", name)
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "datasense",
            audience: "datasense-api",
            claims: claims,
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: creds
        );

        var jwtApiKey = new JwtSecurityTokenHandler().WriteToken(token);

        // Get user's subscription plan
        var userSubscription = await _subscriptionService.GetUserSubscriptionAsync(userId);
        var subscriptionPlanId = userSubscription?.SubscriptionPlanId;

        // Store API key info in database (for tracking and revocation)
        var apiKeyEntity = new ApiKey
        {
            Id = keyId,
            UserId = userId,
            KeyHash = keyHash,
            Name = name,
            UserMetadata = metadata,
            CreatedAt = DateTime.UtcNow,
            SubscriptionPlanId = subscriptionPlanId
        };

        await _repository.CreateAsync(apiKeyEntity);

        _logger.LogInformation("API key generated for user: {UserId}", userId);

        return jwtApiKey;
    }

    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(apiKey, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "datasense",
                ValidateAudience = true,
                ValidAudience = "datasense-api",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == "userId").Value;
            var apiKeyId = jwtToken.Claims.FirstOrDefault(x => x.Type == "keyId")?.Value;

            // Check if API key is still active in database
            var keyHash = HashApiKey(apiKey);
            var apiKeyEntity = await _repository.GetByKeyHashAsync(keyHash);

            if (apiKeyEntity == null || !apiKeyEntity.IsActive || (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt < DateTime.UtcNow))
            {
                return new ApiKeyValidationResult { Success = false };
            }

            return new ApiKeyValidationResult { Success = true, UserId = userId, ApiKeyId = apiKeyId };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API key validation failed");
            return new ApiKeyValidationResult { Success = false };
        }
    }

    public async Task<ApiKey?> GetApiKeyByIdAsync(string apiKeyId)
    {
        return await _repository.GetByIdAsync(apiKeyId);
    }

    public async Task<bool> RevokeApiKeyAsync(string apiKeyId)
    {
        var apiKey = await _repository.GetByIdAsync(apiKeyId);
        if (apiKey == null)
            return false;

        apiKey.IsActive = false;
        return await _repository.UpdateAsync(apiKey);
    }

    public string RegenerateApiKeyToken(ApiKey apiKey)
    {
        // Regenerate JWT token from stored claims
        var claims = new List<Claim>
        {
            new Claim("userId", apiKey.UserId),
            new Claim("keyId", apiKey.Id),
            new Claim("name", apiKey.Name)
        };

        if (apiKey.UserMetadata != null)
        {
            foreach (var kvp in apiKey.UserMetadata)
            {
                claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "datasense",
            audience: "datasense-api",
            claims: claims,
            expires: DateTime.UtcNow.AddYears(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

