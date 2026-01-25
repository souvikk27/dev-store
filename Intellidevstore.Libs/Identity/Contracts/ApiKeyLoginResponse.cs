namespace Intellidevstore.Libs.Identity.Contracts;

public record ApiKeyLoginResponse(string ApiKey, DateTime ExpiresAt, UserInfo User);
