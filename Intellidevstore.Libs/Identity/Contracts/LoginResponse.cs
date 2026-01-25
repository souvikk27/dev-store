namespace Intellidevstore.Libs.Identity.Contracts;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    UserInfo User
);

public record UserInfo(
    Guid Id,
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed
);
