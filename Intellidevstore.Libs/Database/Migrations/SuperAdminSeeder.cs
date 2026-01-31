using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Migrations;

/// <summary>
/// Seeds the super admin user during initial database setup.
/// </summary>
public class SuperAdminSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<SuperAdminSeeder> _logger;

    public SuperAdminSeeder(
        ApplicationDbContext context,
        IPasswordHasherService passwordHasher,
        ILogger<SuperAdminSeeder> logger
    )
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the super admin user if it doesn't exist.
    /// </summary>
    public async Task<SuperAdminSeedResult> SeedAsync(SuperAdminSeedOptions options)
    {
        _logger.LogInformation("Checking for existing super admin user...");

        try
        {
            // Check if super admin already exists
            var existingSuperAdmin = await _context
                .Users.Include(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.UserName == options.UserName || u.Email == options.Email
                );

            if (existingSuperAdmin != null)
            {
                _logger.LogInformation(
                    "Super admin user already exists with ID: {UserId}",
                    existingSuperAdmin.Id
                );

                return SuperAdminSeedResult.Exists(existingSuperAdmin.Id);
            }

            _logger.LogInformation("Creating super admin user...");

            // Get or create the super admin role
            var superAdminRole = await GetOrCreateSuperAdminRoleAsync(options.CreatedBy);

            // Create the super admin user
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var now = DateTime.UtcNow;

            var superAdmin = new User(Guid.NewGuid(), systemUserId)
            {
                UserName = options.UserName,
                Email = options.Email,
                FirstName = options.FirstName,
                LastName = options.LastName,
                PasswordHash = _passwordHasher.HashPassword(options.Password),
                EmailConfirmed = true,
                IsActive = true,
                RequiresMfa = false,
                MfaEnabled = false,
                FailedLoginAttempts = 0,
                IsLockedOut = false,
                CreatedDate = now,
                CreatedBy = systemUserId,
            };

            // Create user-role association
            var userRole = new UserRole(superAdmin.Id, superAdminRole.Id, systemUserId)
            {
                CreatedDate = now,
                CreatedBy = systemUserId,
            };

            await _context.Users.AddAsync(superAdmin);
            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Super admin user created successfully with ID: {UserId}",
                superAdmin.Id
            );

            return SuperAdminSeedResult.Success(superAdmin.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed super admin user");
            return SuperAdminSeedResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Gets the existing super admin role or creates it if it doesn't exist.
    /// </summary>
    private async Task<Role> GetOrCreateSuperAdminRoleAsync(Guid createdBy)
    {
        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "super_admin");

        if (superAdminRole != null)
        {
            return superAdminRole;
        }

        _logger.LogInformation("Creating super admin role...");

        var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var now = DateTime.UtcNow;

        superAdminRole = new Role(Guid.NewGuid(), systemUserId)
        {
            Name = "Super Admin",
            NormalizedName = "SUPER_ADMIN",
            Code = "super_admin",
            Level = 100,
            IsSystem = true,
            Description = "Full system access with all permissions",
            CreatedDate = now,
            CreatedBy = systemUserId,
        };

        await _context.Roles.AddAsync(superAdminRole);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Super admin role created with ID: {RoleId}", superAdminRole.Id);

        return superAdminRole;
    }
}

/// <summary>
/// Options for seeding the super admin user.
/// </summary>
public class SuperAdminSeedOptions
{
    /// <summary>
    /// Username for the super admin account.
    /// </summary>
    public string UserName { get; set; } = "superadmin";

    /// <summary>
    /// Email for the super admin account.
    /// </summary>
    public string Email { get; set; } = "superadmin@localhost";

    /// <summary>
    /// First name of the super admin.
    /// </summary>
    public string FirstName { get; set; } = "Super";

    /// <summary>
    /// Last name of the super admin.
    /// </summary>
    public string LastName { get; set; } = "Admin";

    /// <summary>
    /// Default password for the super admin account.
    /// </summary>
    public string Password { get; set; } = "Admin@123!";

    /// <summary>
    /// ID of the user creating the super admin.
    /// </summary>
    public Guid CreatedBy { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");
}

/// <summary>
/// Result of super admin seeding operation.
/// </summary>
public class SuperAdminSeedResult
{
    public bool IsSuccess { get; set; }
    public bool UserAlreadyExists { get; set; }
    public Guid? UserId { get; set; }
    public string? ErrorMessage { get; set; }

    public static SuperAdminSeedResult Success(Guid userId)
    {
        return new SuperAdminSeedResult { IsSuccess = true, UserId = userId };
    }

    public static SuperAdminSeedResult Exists(Guid userId)
    {
        return new SuperAdminSeedResult
        {
            IsSuccess = true,
            UserAlreadyExists = true,
            UserId = userId,
        };
    }

    public static SuperAdminSeedResult Failure(string errorMessage)
    {
        return new SuperAdminSeedResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
