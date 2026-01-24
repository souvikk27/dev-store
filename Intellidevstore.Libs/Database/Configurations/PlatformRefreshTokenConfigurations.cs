using Intellidevstore.Libs.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class PlatformRefreshTokenConfigurations : IEntityTypeConfiguration<PlatformRefreshToken>
{
    public void Configure(EntityTypeBuilder<PlatformRefreshToken> builder)
    {
        // Table configuration
        builder.ToTable("platform_refresh_tokens");

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

        // ========== PlatformRefreshToken Properties ==========
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Token).IsRequired().HasMaxLength(512);

        builder.Property(x => x.JwtId).HasMaxLength(256);

        builder.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.IsRevoked).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.ExpiryDate).IsRequired();

        builder.Property(x => x.RevokedDate);

        builder.Property(x => x.ReasonForRevocation).HasMaxLength(256);

        builder.Property(x => x.DeviceInfo).HasMaxLength(500);

        builder.Property(x => x.IpAddress).HasMaxLength(45);

        // ========== Indexes ==========
        builder
            .HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("IX_PlatformRefreshTokens_Token");

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_PlatformRefreshTokens_UserId");

        builder.HasIndex(x => x.JwtId).HasDatabaseName("IX_PlatformRefreshTokens_JwtId");

        builder.HasIndex(x => x.ExpiryDate).HasDatabaseName("IX_PlatformRefreshTokens_ExpiryDate");

        builder.HasIndex(x => x.IsRevoked).HasDatabaseName("IX_PlatformRefreshTokens_IsRevoked");

        // ========== Navigation Properties / Relationships ==========
        builder
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filter to exclude tokens for soft-deleted users
        builder.HasQueryFilter(rt => rt.User == null || !rt.User.IsDeleted);

        // Ignore domain events (not persisted)
        builder.Ignore(x => x.DomainEvents);
    }
}
