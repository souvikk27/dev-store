# Authentication PolicyScheme Fix

## Problem

The application was throwing `System.NotImplementedException` when accessing the login endpoint:

```
System.NotImplementedException: The method or operation is not implemented.
at Microsoft.AspNetCore.Authentication.PolicySchemeHandler.HandleAuthenticateAsync()
```

## Root Cause

The `PolicyScheme` authentication handler's `ForwardDefaultSelector` was returning `null` when no authentication headers were present. When `null` is returned, the PolicyScheme doesn't know which authentication scheme to use and throws `NotImplementedException`.

## Solution

Modified the authentication configuration in `ServiceExtensions.cs`:

### Changes Made:

1. **Default to Bearer scheme** instead of returning `null`:

```csharp
options.ForwardDefaultSelector = context =>
{
    if (context.Request.Headers.ContainsKey("X-API-KEY"))
        return "ApiKey";

    if (context.Request.Headers.ContainsKey("Authorization"))
        return "Bearer";

    // Default to Bearer for unauthenticated requests
    return "Bearer";  // ✅ Changed from: return null;
};
```

2. **Added OnMessageReceived event** to allow requests without Authorization header:

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        // Allow requests without Authorization header to pass through
        if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
        {
            context.NoResult();
        }
        return Task.CompletedTask;
    }
};
```

3. **Disabled HTTPS requirement** for development:

```csharp
options.RequireHttpsMetadata = false;
```

## How It Works Now

### Request Flow:

1. **With API Key Header (`X-API-KEY`)**:
   - PolicyScheme forwards to `ApiKey` authentication handler
   - ApiKeyAuthenticationHandler validates the key

2. **With Authorization Header (`Bearer token`)**:
   - PolicyScheme forwards to `Bearer` authentication handler
   - JwtBearerHandler validates the JWT token

3. **Without Authentication Headers**:
   - PolicyScheme forwards to `Bearer` authentication handler (default)
   - Bearer handler's `OnMessageReceived` event calls `context.NoResult()`
   - Request proceeds without authentication
   - Endpoints can handle authorization as needed

## Benefits

- ✅ No more `NotImplementedException`
- ✅ Public endpoints (like login, register) work without authentication
- ✅ Protected endpoints can still require authentication
- ✅ Supports both JWT and API Key authentication
- ✅ Graceful handling of unauthenticated requests

## Testing

The fix has been verified:

- Application starts successfully
- No EF Core warnings
- Login endpoint returns 401 for invalid credentials (expected behavior)
- No exceptions thrown for unauthenticated requests

## Additional Configuration

Added authorization policy for API key-only endpoints:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKeyOnly", p => p.RequireClaim("auth_type", "api_key"));
});
```

## Usage in Endpoints

Endpoints can now optionally require authentication:

```csharp
// Public endpoint (no authentication required)
[WolverinePost("/api/v1/auth/login")]
public static async Task<IResult> Login(LoginRequest request, ...)
{
    // Anyone can access
}

// Protected endpoint (requires authentication)
[Authorize]
[WolverineGet("/api/v1/profile")]
public static IResult GetProfile(ICurrentUserService currentUser)
{
    // Only authenticated users can access
}

// API Key only endpoint
[Authorize(Policy = "ApiKeyOnly")]
[WolverinePost("/api/v1/data/import")]
public static IResult ImportData(...)
{
    // Only API key authenticated requests
}
```

## Related Files Modified

- `intelli-dev-store/Extensions/ServiceExtensions.cs` - Authentication configuration
- No database changes required
- No migration needed

## Status

✅ **FIXED** - Authentication is now working correctly without exceptions.
