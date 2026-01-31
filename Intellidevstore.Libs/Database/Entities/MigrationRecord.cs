using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Database.Entities;

/// <summary>
/// Tracks applied database migrations to prevent duplicate execution.
/// </summary>
public class MigrationRecord : BaseEntity
{
    /// <summary>
    /// Unique identifier for the migration (e.g., "20260131160000_AddUserTable")
    /// </summary>
    public string MigrationId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the migration
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when the migration was applied
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// Duration of migration execution in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Indicates if the migration was applied successfully
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Checksum of migration content for integrity verification
    /// </summary>
    public string? Checksum { get; set; }

    protected MigrationRecord() { }

    public MigrationRecord(Guid id, Guid createdBy)
        : base(id, createdBy) { }
}
