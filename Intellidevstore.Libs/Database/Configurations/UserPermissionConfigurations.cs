using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class UserPermissionConfigurations : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        // Table configuration
        builder.ToTable("user_permissions");

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

        // ========== UserPermission Properties ==========
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.PermissionId).IsRequired();

        builder.Property(x => x.GrantedDate).IsRequired();

        builder.Property(x => x.GrantedBy).IsRequired();

        builder.Property(x => x.ExpiryDate);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.Notes).HasMaxLength(500);

        // ========== Indexes ==========
        builder
            .HasIndex(x => new { x.UserId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_UserPermissions_UserId_PermissionId");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_UserPermissions_UserId");

        builder.HasIndex(x => x.PermissionId).HasDatabaseName("IX_UserPermissions_PermissionId");

        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_UserPermissions_IsActive");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasOne(up => up.User)
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude user permissions for soft-deleted users or permissions
        builder.HasQueryFilter(up =>
            (up.User == null || !up.User.IsDeleted)
            && (up.Permission == null || !up.Permission.IsDeleted)
        );

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
