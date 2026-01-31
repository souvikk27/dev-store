using Intellidevstore.Libs.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class MigrationLockConfigurations : IEntityTypeConfiguration<MigrationLock>
{
    public void Configure(EntityTypeBuilder<MigrationLock> builder)
    {
        // Table configuration
        builder.ToTable("migration_locks");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // ========== Migration Lock Properties ==========
        builder.Property(x => x.LockName).IsRequired().HasMaxLength(100);

        builder.Property(x => x.LockId).IsRequired().HasMaxLength(255);

        builder.Property(x => x.AcquiredAt).IsRequired();

        builder.Property(x => x.ExpiresAt).IsRequired();

        // ========== Indexes ==========
        // Unique index on LockName to prevent multiple locks
        builder
            .HasIndex(x => x.LockName)
            .IsUnique()
            .HasDatabaseName("ix_migration_locks_lock_name");

        // Index on LockId for quick lookup
        builder.HasIndex(x => x.LockId).HasDatabaseName("ix_migration_locks_lock_id");

        // Index on ExpiresAt for cleanup queries
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_migration_locks_expires_at");
    }
}
