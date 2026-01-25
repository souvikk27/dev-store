using System.Security.Cryptography;
using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Identity.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly ApplicationDbContext _context;

    public ApiKeyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string GenerateApiKey()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return $"isk_{Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "")}";
    }

    public async Task<string> StoreApiKeyAsync(Guid userId, DateTime expiresAt)
    {
        var apiKey = GenerateApiKey();

        // Store as a refresh token with special marker
        var apiKeyToken = new PlatformRefreshToken(
            Guid.NewGuid(),
            userId,
            apiKey,
            expiresAt,
            userId
        )
        {
            JwtId = "API_KEY",
            DeviceInfo = "API Key Authentication",
        };

        _context.PlatformRefreshTokens.Add(apiKeyToken);
        await _context.SaveChangesAsync();

        return apiKey;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, Guid userId)
    {
        var storedKey = await _context.PlatformRefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == apiKey
            && rt.UserId == userId
            && rt.JwtId == "API_KEY"
            && !rt.IsRevoked
            && !rt.IsUsed
            && rt.ExpiryDate > DateTime.UtcNow
        );

        return storedKey != null;
    }

    public async Task RevokeApiKeyAsync(string apiKey)
    {
        var storedKey = await _context.PlatformRefreshTokens.FirstOrDefaultAsync(rt =>
            rt.Token == apiKey && rt.JwtId == "API_KEY"
        );

        if (storedKey != null)
        {
            storedKey.IsRevoked = true;
            storedKey.RevokedDate = DateTime.UtcNow;
            storedKey.ReasonForRevocation = "API Key revoked";
            storedKey.SetModified(storedKey.UserId);
            await _context.SaveChangesAsync();
        }
    }
}
