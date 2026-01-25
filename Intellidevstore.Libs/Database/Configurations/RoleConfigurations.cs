using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class RoleConfigurations : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table configuration
        builder.ToTable("roles");

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
        builder.HasQueryFilter(r => !r.IsDeleted);

        // ========== Role Properties ==========
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.NormalizedName).HasMaxLength(100);

        builder.Property(x => x.Code).HasMaxLength(50);

        builder.Property(x => x.Level).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.IsSystem).IsRequired().HasDefaultValue(false);

        // ========== Indexes ==========
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("IX_Roles_Name");

        builder
            .HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_NormalizedName");

        builder.HasIndex(x => x.Code).HasDatabaseName("IX_Roles_Code");

        builder.HasIndex(x => x.IsDeleted).HasDatabaseName("IX_Roles_IsDeleted");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
