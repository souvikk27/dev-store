using System.Security.Claims;
using Intellidevstore.Libs.Identity.Entities;

namespace Intellidevstore.Libs.Identity.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<Claim>? additionalClaims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    string? GetJwtIdFromToken(string token);
}
