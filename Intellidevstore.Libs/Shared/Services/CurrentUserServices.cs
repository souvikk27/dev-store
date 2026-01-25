using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Intellidevstore.Libs.Shared.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// User ID from JWT sub claim.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// User email from JWT email claim.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// User full name from JWT name claim.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Role ID from JWT role_id claim.
    /// </summary>
    Guid RoleId { get; }

    /// <summary>
    /// Role code from JWT role_code claim (e.g., "super_admin").
    /// </summary>
    string RoleCode { get; }

    /// <summary>
    /// Role level from JWT role_level claim (e.g., 100 for SuperAdmin).
    /// </summary>
    int RoleLevel { get; }

    /// <summary>
    /// Authentication type: "platform_user" or "api_key".
    /// </summary>
    string AuthType { get; }

    /// <summary>
    /// List of scopes for API key authentication.
    /// </summary>
    IReadOnlyList<string>? Scopes { get; }

    /// <summary>
    /// True if authenticated via API key.
    /// </summary>
    bool IsApiKey { get; }

    /// <summary>
    /// True if the user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Check if user has a specific scope (for API key auth).
    /// </summary>
    bool HasScope(string scope);

    /// <summary>
    /// Check if user has at least the specified role level.
    /// </summary>
    bool HasMinimumRoleLevel(int requiredLevel);

    /// <summary>
    /// Client IP address from request context.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// User agent from request context.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Correlation ID for request tracing.
    /// </summary>
    string? CorrelationId { get; }
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly HttpContext? _context;
    private readonly ClaimsPrincipal? _user;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _context = httpContextAccessor.HttpContext;
        _user = _context?.User;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim =
                _user?.FindFirst(ClaimTypes.NameIdentifier)
                ?? _user?.FindFirst("sub")
                ?? _user?.FindFirst("user_id");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return Guid.Empty;
        }
    }

    public string? Email
    {
        get
        {
            return _user?.FindFirst(ClaimTypes.Email)?.Value ?? _user?.FindFirst("email")?.Value;
        }
    }

    public string? Name
    {
        get { return _user?.FindFirst(ClaimTypes.Name)?.Value ?? _user?.FindFirst("name")?.Value; }
    }

    public Guid RoleId
    {
        get
        {
            var roleIdClaim = _user?.FindFirst("role_id");

            if (roleIdClaim != null && Guid.TryParse(roleIdClaim.Value, out var roleId))
            {
                return roleId;
            }

            return Guid.Empty;
        }
    }

    public string RoleCode
    {
        get
        {
            return _user?.FindFirst("role_code")?.Value
                ?? _user?.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;
        }
    }

    public int RoleLevel
    {
        get
        {
            var roleLevelClaim = _user?.FindFirst("role_level");

            if (roleLevelClaim != null && int.TryParse(roleLevelClaim.Value, out var roleLevel))
            {
                return roleLevel;
            }

            return 0;
        }
    }

    public string AuthType
    {
        get
        {
            return _user?.FindFirst("auth_type")?.Value
                ?? _user?.FindFirst("AuthType")?.Value
                ?? "platform_user";
        }
    }

    public IReadOnlyList<string>? Scopes
    {
        get
        {
            var scopesClaim = _user?.FindFirst("scopes")?.Value;

            if (string.IsNullOrEmpty(scopesClaim))
            {
                return null;
            }

            // Scopes can be comma-separated or space-separated
            var scopes = scopesClaim
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

            return scopes.AsReadOnly();
        }
    }

    public bool IsApiKey => AuthType.Equals("api_key", StringComparison.OrdinalIgnoreCase);

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress
    {
        get
        {
            // Try to get IP from X-Forwarded-For header (for proxies/load balancers)
            var forwardedFor = _context?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',').FirstOrDefault()?.Trim();
            }

            // Try to get IP from X-Real-IP header
            var realIp = _context?.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to remote IP address
            return _context?.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent
    {
        get { return _context?.Request.Headers.UserAgent.ToString(); }
    }

    public string? CorrelationId
    {
        get
        {
            // Try to get correlation ID from header
            var correlationId =
                _context?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? _context?.Request.Headers["X-Request-ID"].FirstOrDefault();

            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }

            // Try to get from trace identifier
            return _context?.TraceIdentifier;
        }
    }

    public bool HasMinimumRoleLevel(int requiredLevel)
    {
        return RoleLevel >= requiredLevel;
    }

    public bool HasScope(string scope)
    {
        if (Scopes == null || Scopes.Count == 0)
        {
            return false;
        }

        return Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}
