namespace Intellidevstore.Libs.Identity.Services;

public interface IApiKeyService
{
    string GenerateApiKey();
    Task<string> StoreApiKeyAsync(Guid userId, DateTime expiresAt);
    Task<bool> ValidateApiKeyAsync(string apiKey, Guid userId);
    Task RevokeApiKeyAsync(string apiKey);
}
