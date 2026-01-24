using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class PermissionConfigurations : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        // Table configuration
        builder.ToTable("permissions");

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
        builder.HasQueryFilter(p => !p.IsDeleted);

        // ========== Permission Properties ==========
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.Resource).HasMaxLength(100);

        builder.Property(x => x.Action).HasMaxLength(50);

        // ========== Indexes ==========
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("IX_Permissions_Name");

        builder
            .HasIndex(x => new { x.Resource, x.Action })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Resource_Action");

        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_Permissions_IsDeleted");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(p => p.UserPermissions)
            .WithOne(up => up.Permission)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
