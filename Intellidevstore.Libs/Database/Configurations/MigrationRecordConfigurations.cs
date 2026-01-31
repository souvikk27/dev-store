using Intellidevstore.Libs.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Intellidevstore.Libs.Database.Configurations;

public class MigrationRecordConfigurations : IEntityTypeConfiguration<MigrationRecord>
{
    public void Configure(EntityTypeBuilder<MigrationRecord> builder)
    {
        // Table configuration
        builder.ToTable("migration_records");

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

        // ========== Migration Record Properties ==========
        builder.Property(x => x.MigrationId).IsRequired().HasMaxLength(255);

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.AppliedAt).IsRequired();

        builder.Property(x => x.ExecutionTimeMs).IsRequired();

        builder.Property(x => x.IsSuccess).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.ErrorMessage);

        builder.Property(x => x.Checksum).HasMaxLength(64);

        // ========== Indexes ==========
        // Unique index on MigrationId to prevent duplicates
        builder
            .HasIndex(x => x.MigrationId)
            .IsUnique()
            .HasDatabaseName("ix_migration_records_migration_id");

        // Index on AppliedAt for querying migration history
        builder.HasIndex(x => x.AppliedAt).HasDatabaseName("ix_migration_records_applied_at");

        // Index on IsSuccess for filtering failed migrations
        builder.HasIndex(x => x.IsSuccess).HasDatabaseName("ix_migration_records_is_success");
    }
}
