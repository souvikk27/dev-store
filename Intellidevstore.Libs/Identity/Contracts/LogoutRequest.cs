namespace Intellidevstore.Libs.Identity.Contracts;

public record LogoutRequest(string? RefreshToken = null, bool LogoutAllDevices = false);
