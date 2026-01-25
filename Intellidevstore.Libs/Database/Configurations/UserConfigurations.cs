using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable("users");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // ========== Base Entity Properties ==========
        builder.Property(x => x.CreatedDate).IsRequired();

        builder.Property(x => x.CreatedBy).IsRequired();

        builder.Property(x => x.ModifiedDate);

        builder.Property(x => x.ModifiedBy);

        // Row version for optimistic concurrency
        builder.Property(x => x.RowVersion).IsRowVersion();

        // ========== Soft Delete Properties ==========
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.DeletedBy);

        // Global query filter for soft delete
        builder.HasQueryFilter(u => !u.IsDeleted);

        // ========== User Properties ==========
        builder.Property(x => x.UserName).IsRequired().HasMaxLength(256);

        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);

        builder.Property(x => x.Phone).HasMaxLength(20);

        builder.Property(x => x.FirstName).HasMaxLength(100);

        builder.Property(x => x.LastName).HasMaxLength(100);

        builder.Property(x => x.PasswordHash).HasMaxLength(512);

        builder.Property(x => x.EmailConfirmed).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.LastLoginDate);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        // ========== MFA Properties ==========
        builder.Property(x => x.RequiresMfa).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.MfaSecret).HasMaxLength(256);

        builder.Property(x => x.MfaEnabled).IsRequired().HasDefaultValue(false);

        // ========== Lockout Properties ==========
        builder.Property(x => x.FailedLoginAttempts).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.IsLockedOut).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.LockoutEndAt);

        // ========== Indexes ==========
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("IX_Users_Email");

        builder.HasIndex(x => x.UserName).IsUnique().HasDatabaseName("IX_Users_UserName");

        builder.HasIndex(x => x.Phone).HasDatabaseName("IX_Users_Phone");

        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_Users_IsDeleted");

        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_Users_CreatedDate");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany<UserPermission>()
            .WithOne(up => up.User)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany<UserSession>()
            .WithOne(us => us.User)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
