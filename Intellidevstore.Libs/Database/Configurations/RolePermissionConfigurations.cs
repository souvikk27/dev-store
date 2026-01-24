using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class RolePermissionConfigurations : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Table configuration
        builder.ToTable("role_permissions");

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

        // ========== RolePermission Properties ==========
        builder.Property(x => x.RoleId).IsRequired();

        builder.Property(x => x.PermissionId).IsRequired();

        builder.Property(x => x.GrantedDate).IsRequired();

        builder.Property(x => x.GrantedBy).IsRequired();

        builder.Property(x => x.ExpiryDate);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.Notes).HasMaxLength(500);

        // ========== Indexes ==========
        builder
            .HasIndex(x => new { x.RoleId, x.PermissionId })
            .IsUnique()
            .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId");

        builder.HasIndex(x => x.RoleId).HasDatabaseName("IX_RolePermissions_RoleId");

        builder.HasIndex(x => x.PermissionId).HasDatabaseName("IX_RolePermissions_PermissionId");

        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_RolePermissions_IsActive");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude role permissions for soft-deleted roles or permissions
        builder.HasQueryFilter(rp =>
            (rp.Role == null || !rp.Role.IsDeleted)
            && (rp.Permission == null || !rp.Permission.IsDeleted)
        );

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
