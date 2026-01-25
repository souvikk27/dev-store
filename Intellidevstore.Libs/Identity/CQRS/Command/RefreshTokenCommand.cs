using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.Services;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record RefreshTokenCommand(RefreshTokenRequest Request);

public sealed class RefreshTokenHandler
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenHandler(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration
    )
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command)
    {
        // Validate the access token and get principal
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(command.Request.AccessToken);

        if (principal == null)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.InvalidToken", "Invalid access token")
            );
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.InvalidToken", "Invalid user ID in token")
            );
        }

        // Get JWT ID from the access token
        var jwtId = _jwtTokenService.GetJwtIdFromToken(command.Request.AccessToken);

        // Find the refresh token in database
        var storedRefreshToken = await _context
            .PlatformRefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == command.Request.RefreshToken && rt.UserId == userId
            );

        if (storedRefreshToken == null)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid refresh token")
            );
        }

        // Validate refresh token
        if (storedRefreshToken.IsUsed)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.RefreshTokenUsed", "Refresh token has already been used")
            );
        }

        if (storedRefreshToken.IsRevoked)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.RefreshTokenRevoked", "Refresh token has been revoked")
            );
        }

        if (storedRefreshToken.ExpiryDate < DateTime.UtcNow)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.RefreshTokenExpired", "Refresh token has expired")
            );
        }

        // Validate JWT ID matches
        if (storedRefreshToken.JwtId != jwtId)
        {
            return Result.Failure<RefreshTokenResponse>(
                Error.Unauthorized("Auth.TokenMismatch", "Token mismatch")
            );
        }

        // Mark the old refresh token as used
        storedRefreshToken.IsUsed = true;
        storedRefreshToken.SetModified(userId);

        // Generate new tokens
        var user = storedRefreshToken.User!;
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newJwtId = _jwtTokenService.GetJwtIdFromToken(newAccessToken);

        var refreshTokenExpirationDays = int.Parse(
            _configuration["JwtSettings:RefreshTokenExpirationDays"] ?? "7"
        );
        var accessTokenExpirationMinutes = int.Parse(
            _configuration["JwtSettings:ExpirationMinutes"] ?? "60"
        );

        var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);
        var newAccessTokenExpiry = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes);

        // Store new refresh token
        var newPlatformRefreshToken = new Entities.PlatformRefreshToken(
            Guid.NewGuid(),
            userId,
            newRefreshToken,
            newRefreshTokenExpiry,
            userId
        )
        {
            JwtId = newJwtId,
            DeviceInfo = storedRefreshToken.DeviceInfo,
            IpAddress = storedRefreshToken.IpAddress,
        };

        _context.PlatformRefreshTokens.Add(newPlatformRefreshToken);
        await _context.SaveChangesAsync();

        var response = new RefreshTokenResponse(
            newAccessToken,
            newRefreshToken,
            newAccessTokenExpiry,
            newRefreshTokenExpiry
        );

        return Result.Success(response);
    }
}
