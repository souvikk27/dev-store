# Role Seeder Guide

## Overview

The Role Seeder automatically populates the database with predefined system roles when the application starts.

## Default Roles

The seeder creates 5 default roles with hierarchical levels:

| Role        | Code          | Level | Is System | Description                                            |
| ----------- | ------------- | ----- | --------- | ------------------------------------------------------ |
| Super Admin | `super_admin` | 100   | Yes       | Full system access with all permissions                |
| Admin       | `admin`       | 50    | Yes       | Administrative access with most permissions            |
| Manager     | `manager`     | 30    | No        | Manager access with limited administrative permissions |
| User        | `user`        | 10    | No        | Standard user access                                   |
| Guest       | `guest`       | 0     | No        | Limited guest access                                   |

## Role Properties

### Level (Hierarchical)

- Higher level = more privileges
- Used for role-based authorization checks
- Example: `currentUser.HasMinimumRoleLevel(50)` allows Admin and Super Admin

### Is System

- System roles cannot be deleted through normal operations
- Protects critical roles from accidental deletion
- Super Admin and Admin are marked as system roles

### Code

- Unique identifier for programmatic role checks
- Used in JWT claims for role identification
- Lowercase with underscores (e.g., `super_admin`)

## Automatic Seeding

The database is automatically seeded when the application starts:

```csharp
// In Program.cs
await app.SeedDatabaseAsync();
```

### Seeding Behavior

- Runs on application startup
- Checks if roles already exist
- Skips seeding if roles are found
- Logs all seeding operations
- Safe to run multiple times (idempotent)

## Manual Seeding

### Option 1: Using the Extension Method

```csharp
using Intellidevstore.Libs.Database.Seeders;

// In your startup code
await app.SeedDatabaseAsync();
```

### Option 2: Direct Seeder Usage

```csharp
using Intellidevstore.Libs.Database.Seeders;

var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>();

var seeder = new RoleSeeder(context, logger);
await seeder.SeedAsync();
```

## Adding Custom Roles

### Method 1: Modify the Seeder

Edit `RoleSeeder.cs` and add your custom role:

```csharp
private static List<Role> GetDefaultRoles(Guid createdBy)
{
    var now = DateTime.UtcNow;

    return new List<Role>
    {
        // ... existing roles ...

        new Role(Guid.NewGuid(), createdBy)
        {
            Name = "Custom Role",
            NormalizedName = "CUSTOM_ROLE",
            Code = "custom_role",
            Level = 25,
            IsSystem = false,
            Description = "Custom role description",
            CreatedDate = now,
        },
    };
}
```

### Method 2: Create a Separate Seeder

```csharp
public class CustomRoleSeeder
{
    private readonly ApplicationDbContext _context;

    public CustomRoleSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        var customRole = new Role(Guid.NewGuid(), systemUserId)
        {
            Name = "Custom Role",
            NormalizedName = "CUSTOM_ROLE",
            Code = "custom_role",
            Level = 25,
            IsSystem = false,
            Description = "Custom role description",
            CreatedDate = DateTime.UtcNow,
        };

        if (!await _context.Roles.AnyAsync(r => r.Code == "custom_role"))
        {
            await _context.Roles.AddAsync(customRole);
            await _context.SaveChangesAsync();
        }
    }
}
```

## Assigning Roles to Users

### During User Creation

```csharp
// Create user
var user = new User(Guid.NewGuid(), createdBy)
{
    UserName = "john.doe",
    Email = "john@example.com",
    // ... other properties
};

await _context.Users.AddAsync(user);

// Get the "User" role
var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "user");

if (userRole != null)
{
    var userRoleAssignment = new UserRole
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        RoleId = userRole.Id,
        CreatedBy = createdBy,
        CreatedDate = DateTime.UtcNow
    };

    await _context.UserRoles.AddAsync(userRoleAssignment);
}

await _context.SaveChangesAsync();
```

### Using a Service

```csharp
public class UserRoleService
{
    private readonly ApplicationDbContext _context;

    public UserRoleService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleCode, Guid assignedBy)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User.NotFound", "User not found"));
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
        if (role == null)
        {
            return Result.Failure(Error.NotFound("Role.NotFound", "Role not found"));
        }

        // Check if already assigned
        var existingAssignment = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

        if (existingAssignment)
        {
            return Result.Failure(Error.Conflict("Role.AlreadyAssigned", "Role already assigned"));
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = role.Id,
            CreatedBy = assignedBy,
            CreatedDate = DateTime.UtcNow
        };

        await _context.UserRoles.AddAsync(userRole);
        await _context.SaveChangesAsync();

        return Result.Success();
    }
}
```

## Querying Roles

### Get All Roles

```csharp
var roles = await _context.Roles
    .OrderByDescending(r => r.Level)
    .ToListAsync();
```

### Get Role by Code

```csharp
var adminRole = await _context.Roles
    .FirstOrDefaultAsync(r => r.Code == "admin");
```

### Get User's Roles

```csharp
var userRoles = await _context.UserRoles
    .Include(ur => ur.Role)
    .Where(ur => ur.UserId == userId)
    .Select(ur => ur.Role)
    .ToListAsync();
```

### Get User's Highest Role

```csharp
var highestRole = await _context.UserRoles
    .Include(ur => ur.Role)
    .Where(ur => ur.UserId == userId)
    .OrderByDescending(ur => ur.Role!.Level)
    .Select(ur => ur.Role)
    .FirstOrDefaultAsync();
```

## Role-Based Authorization

### Using CurrentUserService

```csharp
[WolverinePost("/api/admin/settings")]
public IResult UpdateSettings(
    UpdateSettingsRequest request,
    ICurrentUserService currentUser
)
{
    // Check if user has admin level (50+)
    if (!currentUser.HasMinimumRoleLevel(50))
    {
        return Results.Forbid();
    }

    // Proceed with update
    return Results.Ok();
}
```

### Using Role Code

```csharp
if (currentUser.RoleCode == "super_admin")
{
    // Super admin only functionality
}
```

### Custom Authorization Policy

```csharp
// In Program.cs or ServiceExtensions.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireClaim("role_code", "admin", "super_admin"));

    options.AddPolicy("RequireMinimumLevel50", policy =>
        policy.RequireAssertion(context =>
        {
            var levelClaim = context.User.FindFirst("role_level");
            if (levelClaim != null && int.TryParse(levelClaim.Value, out var level))
            {
                return level >= 50;
            }
            return false;
        }));
});

// Usage in endpoint
[Authorize(Policy = "RequireAdminRole")]
[WolverineGet("/api/admin/users")]
public IResult GetAllUsers()
{
    // Only admins can access
}
```

## Database Schema

### Roles Table

```sql
CREATE TABLE roles (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    normalized_name VARCHAR(100),
    code VARCHAR(50),
    level INTEGER NOT NULL DEFAULT 0,
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    description VARCHAR(500),
    created_date TIMESTAMP NOT NULL,
    created_by UUID NOT NULL,
    modified_date TIMESTAMP,
    modified_by UUID,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    deleted_by UUID,
    row_version BYTEA
);

CREATE UNIQUE INDEX IX_Roles_Name ON roles(name);
CREATE UNIQUE INDEX IX_Roles_NormalizedName ON roles(normalized_name);
CREATE INDEX IX_Roles_Code ON roles(code);
CREATE INDEX IX_Roles_IsDeleted ON roles(is_deleted);
```

## Troubleshooting

### Roles Not Seeding

1. Check database connection
2. Verify migrations are applied
3. Check application logs for errors
4. Ensure `SeedDatabaseAsync()` is called in Program.cs

### Duplicate Role Errors

The seeder checks if roles exist before seeding. If you get duplicate errors:

1. Clear the roles table
2. Restart the application
3. Or manually run: `DELETE FROM roles;`

### System Role Protection

To prevent deletion of system roles, add a check:

```csharp
public async Task<Result> DeleteRoleAsync(Guid roleId)
{
    var role = await _context.Roles.FindAsync(roleId);

    if (role == null)
    {
        return Result.Failure(Error.NotFound("Role.NotFound", "Role not found"));
    }

    if (role.IsSystem)
    {
        return Result.Failure(
            Error.Forbidden("Role.SystemProtected", "System roles cannot be deleted")
        );
    }

    role.Delete(currentUserId);
    await _context.SaveChangesAsync();

    return Result.Success();
}
```

## Best Practices

1. **Don't modify system roles** - Create custom roles instead
2. **Use role levels** for hierarchical permissions
3. **Use role codes** for programmatic checks
4. **Assign default role** to new users (usually "user")
5. **Log role changes** for audit trail
6. **Validate role assignments** before saving
7. **Cache role lookups** for better performance
8. **Use soft delete** to maintain referential integrity

## Example: Complete User Registration with Role

```csharp
public async Task<Result<User>> RegisterUserAsync(CreateUserRequest request)
{
    // Create user
    var user = new User(Guid.NewGuid(), Guid.Empty)
    {
        UserName = request.UserName,
        Email = request.Email,
        PasswordHash = _passwordHasher.HashPassword(request.Password),
        FirstName = request.FirstName,
        LastName = request.LastName,
        IsActive = true,
        CreatedDate = DateTime.UtcNow
    };

    await _context.Users.AddAsync(user);

    // Assign default "user" role
    var defaultRole = await _context.Roles
        .FirstOrDefaultAsync(r => r.Code == "user");

    if (defaultRole != null)
    {
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = defaultRole.Id,
            CreatedBy = user.Id,
            CreatedDate = DateTime.UtcNow
        };

        await _context.UserRoles.AddAsync(userRole);
    }

    await _context.SaveChangesAsync();

    return Result.Success(user);
}
```
