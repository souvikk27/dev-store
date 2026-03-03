using System.Security.Claims;
using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Identity.Services;
using Intellidevstore.Libs.Messaging.Command;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record LoginCommand(
    LoginRequest Request,
    string IpAddress,
    string UserAgent,
    string DeviceInfo,
    string GrantType
) : ICommand<Result<object>>;

public sealed class LoginHandler(
    ApplicationDbContext context,
    IPasswordHasherService passwordHasherService,
    IJwtTokenService jwtTokenService,
    IApiKeyService apiKeyService,
    IConfiguration configuration
) : ICommandHandler<LoginCommand, Result<object>>
{
    public async Task<Result<object>> Handle(LoginCommand command, CancellationToken ct = default)
    {
        // Find user by email or username
        var user = await context
            .Users.Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
            .Where(u =>
                u.Email != null
                && u.UserName != null
                && (
                    u.Email.ToLower() == command.Request.EmailOrUsername.ToLower()
                    || u.UserName.ToLower() == command.Request.EmailOrUsername.ToLower()
                )
            )
            .FirstOrDefaultAsync(cancellationToken: ct);

        if (user == null)
        {
            return Result.Failure<object>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email/username or password")
            );
        }

        // Check if user is locked out
        if (
            user is { IsLockedOut: true, LockoutEndAt: not null }
            && user.LockoutEndAt > DateTime.UtcNow
        )
        {
            return Result.Failure<object>(
                Error.Forbidden(
                    "Auth.AccountLocked",
                    $"Account is locked until {user.LockoutEndAt.Value:yyyy-MM-dd HH:mm:ss} UTC"
                )
            );
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Failure<object>(
                Error.Forbidden("Auth.AccountInactive", "Account is inactive")
            );
        }

        // Verify password
        var isPasswordValid = passwordHasherService.VerifyPassword(
            command.Request.Password,
            user.PasswordHash ?? string.Empty
        );

        if (!isPasswordValid)
        {
            // Increment failed login attempts
            user.FailedLoginAttempts++;

            // Lock account after 5 failed attempts
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLockedOut = true;
                user.LockoutEndAt = DateTime.UtcNow.AddMinutes(30);
            }

            user.SetModified(user.Id);
            await context.SaveChangesAsync(ct);

            return Result.Failure<object>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email/username or password")
            );
        }

        // Reset failed login attempts and lockout on successful login
        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndAt = null;
        user.LastLoginDate = DateTime.UtcNow;
        user.SetModified(user.Id);

        var userInfo = new UserInfo(
            user.Id,
            user.UserName,
            user.Email,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed
        );

        // Handle different grant types
        if (command.GrantType.Equals("api_key", StringComparison.OrdinalIgnoreCase))
        {
            // Generate API Key
            var apiKeyExpirationDays = int.Parse(
                configuration["JwtSettings:ApiKeyExpirationDays"] ?? "365"
            );
            var apiKeyExpiry = DateTime.UtcNow.AddDays(apiKeyExpirationDays);

            var apiKey = await apiKeyService.StoreApiKeyAsync(user.Id, apiKeyExpiry);

            // Create user session for API key
            var apiKeySession = new UserSession(
                Guid.NewGuid(),
                user.Id,
                apiKey,
                command.DeviceInfo,
                command.IpAddress,
                command.UserAgent,
                user.Id
            );

            context.UserSessions.Add(apiKeySession);
            await context.SaveChangesAsync(ct);

            var apiKeyResponse = new ApiKeyLoginResponse(apiKey, apiKeyExpiry, userInfo);

            return Result.Success<object>(apiKeyResponse);
        }
        else // Default to refresh_token grant type
        {
            // Get user's primary role (highest level)
            var primaryRole = user
                .UserRoles?.OrderByDescending(ur => ur.Role?.Level ?? 0)
                .FirstOrDefault()
                ?.Role;

            // Build additional claims for role information
            var additionalClaims = new List<Claim> { new("auth_type", "platform_user") };

            if (primaryRole != null)
            {
                additionalClaims.Add(new Claim("role_id", primaryRole.Id.ToString()));
                additionalClaims.Add(
                    new Claim("role_code", primaryRole.Code ?? primaryRole.Name ?? string.Empty)
                );
                additionalClaims.Add(new Claim("role_level", primaryRole.Level.ToString()));
                additionalClaims.Add(new Claim(ClaimTypes.Role, primaryRole.Name ?? string.Empty));
            }

            // Generate JWT tokens with role claims
            var accessToken = jwtTokenService.GenerateAccessToken(user, additionalClaims);
            var refreshToken = jwtTokenService.GenerateRefreshToken();
            var jwtId = jwtTokenService.GetJwtIdFromToken(accessToken);

            var refreshTokenExpirationDays = int.Parse(
                configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"
            );
            var accessTokenExpirationMinutes = int.Parse(
                configuration["JwtSettings:ExpirationMinutes"] ?? "60"
            );

            var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes);

            // Store refresh token
            var platformRefreshToken = new PlatformRefreshToken(
                Guid.NewGuid(),
                user.Id,
                refreshToken,
                refreshTokenExpiry,
                user.Id
            )
            {
                JwtId = jwtId,
                DeviceInfo = command.DeviceInfo,
                IpAddress = command.IpAddress,
            };

            context.PlatformRefreshTokens.Add(platformRefreshToken);

            await context.SaveChangesAsync(ct);

            var tokenResponse = new LoginResponse(
                accessToken,
                refreshToken,
                accessTokenExpiry,
                refreshTokenExpiry,
                userInfo
            );

            return Result.Success<object>(tokenResponse);
        }
    }
}
