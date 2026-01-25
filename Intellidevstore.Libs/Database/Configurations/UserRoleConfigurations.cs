using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class UserRoleConfigurations : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table configuration
        builder.ToTable("user_roles");

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

        // ========== UserRole Properties ==========
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.RoleId).IsRequired();

        builder.Property(x => x.AssignedDate).IsRequired();

        builder.Property(x => x.AssignedBy).IsRequired();

        builder.Property(x => x.Notes).HasMaxLength(500);

        // ========== Indexes ==========
        builder
            .HasIndex(x => new { x.UserId, x.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleId");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(x => x.RoleId).HasDatabaseName("IX_UserRoles_RoleId");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude user roles for soft-deleted users or roles
        builder.HasQueryFilter(ur =>
            (ur.User == null || !ur.User.IsDeleted) && (ur.Role == null || !ur.Role.IsDeleted)
        );

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
