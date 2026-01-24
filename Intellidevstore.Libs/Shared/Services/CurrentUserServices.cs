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
    /// List of tenant IDs this user can access (for restricted roles).
    /// </summary>
    IReadOnlyList<string>? AssignedTenantIds { get; }

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
