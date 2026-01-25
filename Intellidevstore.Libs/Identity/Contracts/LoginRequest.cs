namespace Intellidevstore.Libs.Identity.Contracts;

public record LoginRequest(string EmailOrUsername, string Password, string? DeviceInfo = null);
