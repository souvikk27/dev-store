# CurrentUserService Implementation Guide

## Overview

The `CurrentUserService` provides a centralized way to access authenticated user information from JWT claims and HTTP context throughout your application.

## Features

### User Identity

- `UserId` - User's unique identifier (Guid)
- `Email` - User's email address
- `Name` - User's display name
- `IsAuthenticated` - Whether the user is authenticated

### Role Information

- `RoleId` - User's primary role ID
- `RoleCode` - Role code (e.g., "super_admin", "admin", "user")
- `RoleLevel` - Hierarchical role level (higher = more privileges)
- `HasMinimumRoleLevel(int)` - Check if user has minimum role level

### Authentication Type

- `AuthType` - "platform_user" or "api_key"
- `IsApiKey` - True if authenticated via API key
- `Scopes` - List of scopes for API key authentication
- `HasScope(string)` - Check if user has specific scope

### Request Context

- `IpAddress` - Client IP address (supports X-Forwarded-For)
- `UserAgent` - Client user agent string
- `CorrelationId` - Request correlation/trace ID

## JWT Claims Structure

The service extracts information from the following JWT claims:

```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "name": "John Doe",
  "role_id": "role-guid",
  "role_code": "admin",
  "role_level": "50",
  "auth_type": "platform_user",
  "scopes": "read,write,delete"
}
```

## Usage Examples

### 1. Basic Usage in Controllers/Endpoints

```csharp
using Intellidevstore.Libs.Shared.Services;

public class MyEndpoint
{
    private readonly ICurrentUserService _currentUser;

    public MyEndpoint(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [WolverineGet("/api/profile")]
    public IResult GetProfile()
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            userId = _currentUser.UserId,
            email = _currentUser.Email,
            name = _currentUser.Name,
            roleCode = _currentUser.RoleCode,
            roleLevel = _currentUser.RoleLevel
        });
    }
}
```

### 2. Role-Based Authorization

```csharp
[WolverinePost("/api/admin/users")]
public async Task<IResult> CreateUser(
    CreateUserRequest request,
    ICurrentUserService currentUser,
    IMessageBus bus
)
{
    // Check if user has admin role level (50+)
    if (!currentUser.HasMinimumRoleLevel(50))
    {
        return Results.Forbid();
    }

    var command = new CreateUserCommand(request, currentUser.UserId);
    var result = await bus.InvokeAsync<Result<User>>(command);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
}
```

### 3. API Key Scope Validation

```csharp
[WolverineDelete("/api/resources/{id}")]
public IResult DeleteResource(
    Guid id,
    ICurrentUserService currentUser
)
{
    // Check if API key has delete scope
    if (currentUser.IsApiKey && !currentUser.HasScope("delete"))
    {
        return Results.Problem(
            statusCode: 403,
            detail: "API key does not have 'delete' scope"
        );
    }

    // Proceed with deletion
    return Results.Ok();
}
```

### 4. Audit Logging

```csharp
public class AuditService
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ICurrentUserService currentUser,
        ILogger<AuditService> logger
    )
    {
        _currentUser = currentUser;
        _logger = logger;
    }

    public void LogAction(string action, object data)
    {
        _logger.LogInformation(
            "User {UserId} ({Email}) performed {Action} from {IpAddress}. " +
            "CorrelationId: {CorrelationId}",
            _currentUser.UserId,
            _currentUser.Email,
            action,
            _currentUser.IpAddress,
            _currentUser.CorrelationId
        );
    }
}
```

### 5. Using in Command Handlers

```csharp
public sealed class UpdateResourceHandler
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateResourceHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUser
    )
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateResourceCommand command)
    {
        var resource = await _context.Resources.FindAsync(command.ResourceId);

        if (resource == null)
        {
            return Result.Failure(Error.NotFound("Resource.NotFound", "Resource not found"));
        }

        // Check ownership or admin privileges
        if (resource.OwnerId != _currentUser.UserId &&
            !_currentUser.HasMinimumRoleLevel(50))
        {
            return Result.Failure(
                Error.Forbidden("Resource.Forbidden", "You don't have permission to update this resource")
            );
        }

        // Update resource
        resource.Name = command.Name;
        resource.SetModified(_currentUser.UserId);

        await _context.SaveChangesAsync();
        return Result.Success();
    }
}
```

### 6. Custom Authorization Attribute

```csharp
public class MinimumRoleLevelAttribute : Attribute
{
    public int RequiredLevel { get; }

    public MinimumRoleLevelAttribute(int requiredLevel)
    {
        RequiredLevel = requiredLevel;
    }
}

// Middleware to check role level
public class RoleLevelMiddleware
{
    private readonly RequestDelegate _next;

    public RoleLevelMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserService currentUser
    )
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<MinimumRoleLevelAttribute>();

        if (attribute != null)
        {
            if (!currentUser.HasMinimumRoleLevel(attribute.RequiredLevel))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Insufficient role level",
                    required = attribute.RequiredLevel,
                    current = currentUser.RoleLevel
                });
                return;
            }
        }

        await _next(context);
    }
}
```

## Role Level Hierarchy Example

```csharp
public static class RoleLevels
{
    public const int SuperAdmin = 100;
    public const int Admin = 50;
    public const int Manager = 30;
    public const int User = 10;
    public const int Guest = 0;
}

// Usage
if (currentUser.HasMinimumRoleLevel(RoleLevels.Admin))
{
    // Admin or SuperAdmin can access
}
```

## API Key Scopes Example

```csharp
public static class ApiScopes
{
    public const string Read = "read";
    public const string Write = "write";
    public const string Delete = "delete";
    public const string Admin = "admin";
}

// Usage
if (currentUser.IsApiKey)
{
    if (!currentUser.HasScope(ApiScopes.Write))
    {
        return Results.Forbid();
    }
}
```

## IP Address Detection

The service checks for IP address in the following order:

1. `X-Forwarded-For` header (for proxies/load balancers)
2. `X-Real-IP` header
3. `RemoteIpAddress` from connection

## Correlation ID Detection

The service checks for correlation ID in the following order:

1. `X-Correlation-ID` header
2. `X-Request-ID` header
3. `TraceIdentifier` from HTTP context

## Testing

### Unit Test Example

```csharp
public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_ReturnsCorrectValue()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        var accessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var service = new CurrentUserService(accessor);

        // Act
        var userId = service.UserId;

        // Assert
        Assert.NotEqual(Guid.Empty, userId);
    }
}
```

## Best Practices

1. **Always check authentication** before accessing user properties
2. **Use role levels** for hierarchical permissions
3. **Use scopes** for fine-grained API key permissions
4. **Log user actions** with correlation IDs for traceability
5. **Handle Guid.Empty** for unauthenticated users
6. **Use in scoped services** (registered as Scoped in DI)

## Registration

The service is automatically registered in `DependencyInjection.cs`:

```csharp
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

## Dependencies

- `Microsoft.AspNetCore.Http` - For HttpContext access
- `System.Security.Claims` - For claims extraction
