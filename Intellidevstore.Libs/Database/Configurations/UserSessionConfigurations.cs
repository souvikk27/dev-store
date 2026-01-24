using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class UserSessionConfigurations : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        // Table configuration
        builder.ToTable("user_sessions");

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

        // ========== UserSession Properties ==========
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.SessionToken).IsRequired().HasMaxLength(512);

        builder.Property(x => x.StartedAt).IsRequired();

        builder.Property(x => x.EndedAt);

        builder.Property(x => x.LastActivityAt);

        builder.Property(x => x.DeviceInfo).HasMaxLength(500);

        builder.Property(x => x.IpAddress).HasMaxLength(45);

        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.Property(x => x.Location).HasMaxLength(256);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.IsEnded).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.ReasonForEnd).HasMaxLength(256);

        // ========== Indexes ==========
        builder
            .HasIndex(x => x.SessionToken)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_SessionToken");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_UserSessions_UserId");

        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_UserSessions_IsActive");

        builder.HasIndex(x => x.StartedAt).HasDatabaseName("IX_UserSessions_StartedAt");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude sessions for soft-deleted users
        builder.HasQueryFilter(us => us.User == null || !us.User.IsDeleted);

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
