namespace Intellidevstore.Libs.Database.Entities;

/// <summary>
/// Entity for table-based migration locking to prevent concurrent migration execution.
/// </summary>
public class MigrationLock
{
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique name of the lock
    /// </summary>
    public string LockName { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the lock holder
    /// </summary>
    public string LockId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the lock was acquired
    /// </summary>
    public DateTime AcquiredAt { get; set; }

    /// <summary>
    /// Timestamp when the lock expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
