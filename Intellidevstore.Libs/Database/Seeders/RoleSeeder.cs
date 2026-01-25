using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Seeders;

public class RoleSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(ApplicationDbContext context, ILogger<RoleSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if roles already exist
            if (await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Roles already seeded. Skipping role seeding.");
                return;
            }

            _logger.LogInformation("Starting role seeding...");

            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var roles = GetDefaultRoles(systemUserId);

            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} roles", roles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding roles");
            throw;
        }
    }

    private static List<Role> GetDefaultRoles(Guid createdBy)
    {
        var now = DateTime.UtcNow;

        return new List<Role>
        {
            new Role(Guid.NewGuid(), createdBy)
            {
                Name = "Super Admin",
                NormalizedName = "SUPER_ADMIN",
                Code = "super_admin",
                Level = 100,
                IsSystem = true,
                Description = "Full system access with all permissions",
                CreatedDate = now,
            },
            new Role(Guid.NewGuid(), createdBy)
            {
                Name = "Admin",
                NormalizedName = "ADMIN",
                Code = "admin",
                Level = 50,
                IsSystem = true,
                Description = "Administrative access with most permissions",
                CreatedDate = now,
            },
            new Role(Guid.NewGuid(), createdBy)
            {
                Name = "Manager",
                NormalizedName = "MANAGER",
                Code = "manager",
                Level = 30,
                IsSystem = false,
                Description = "Manager access with limited administrative permissions",
                CreatedDate = now,
            },
            new Role(Guid.NewGuid(), createdBy)
            {
                Name = "User",
                NormalizedName = "USER",
                Code = "user",
                Level = 10,
                IsSystem = false,
                Description = "Standard user access",
                CreatedDate = now,
            },
            new Role(Guid.NewGuid(), createdBy)
            {
                Name = "Guest",
                NormalizedName = "GUEST",
                Code = "guest",
                Level = 0,
                IsSystem = false,
                Description = "Limited guest access",
                CreatedDate = now,
            },
        };
    }
}
