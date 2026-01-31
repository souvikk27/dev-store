using Intellidevstore.Libs.Database.Entities;
using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options) { }

    // Identity entities
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<PlatformRefreshToken> PlatformRefreshTokens { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    // Migration tracking entity
    public DbSet<MigrationRecord> MigrationRecords { get; set; }

    // Migration locking entity
    public DbSet<MigrationLock> MigrationLocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply all configurations declared in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
