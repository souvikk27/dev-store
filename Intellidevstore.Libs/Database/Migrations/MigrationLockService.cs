using Intellidevstore.Libs.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Migrations;

/// <summary>
/// Service that provides distributed locking for migrations to prevent concurrent execution.
/// </summary>
public class MigrationLockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MigrationLockService> _logger;
    private readonly TimeSpan _lockTimeout;
    private readonly string _lockId;
    private bool _isLocked;

    public MigrationLockService(
        ApplicationDbContext context,
        ILogger<MigrationLockService> logger,
        TimeSpan? lockTimeout = null
    )
    {
        _context = context;
        _logger = logger;
        _lockTimeout = lockTimeout ?? TimeSpan.FromMinutes(5);
        _lockId = $"migration_lock_{Environment.MachineName}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Attempts to acquire the migration lock.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if lock was acquired, false otherwise</returns>
    public async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempting to acquire migration lock with ID: {LockId}", _lockId);

        try
        {
            // Use advisory lock if available (PostgreSQL specific)
            if (await TryAcquireAdvisoryLockAsync(cancellationToken))
            {
                _isLocked = true;
                _logger.LogInformation("Migration lock acquired successfully");
                return true;
            }

            // Fallback to table-based locking
            if (await TryAcquireTableLockAsync(cancellationToken))
            {
                _isLocked = true;
                _logger.LogInformation("Migration table lock acquired successfully");
                return true;
            }

            _logger.LogWarning(
                "Failed to acquire migration lock - another process may be running migrations"
            );
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring migration lock");
            return false;
        }
    }

    /// <summary>
    /// Releases the migration lock.
    /// </summary>
    public async Task ReleaseLockAsync()
    {
        if (!_isLocked)
        {
            return;
        }

        _logger.LogDebug("Releasing migration lock");

        try
        {
            // Release advisory lock
            await ReleaseAdvisoryLockAsync();

            // Release table lock
            await ReleaseTableLockAsync();

            _isLocked = false;
            _logger.LogInformation("Migration lock released successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing migration lock");
        }
    }

    /// <summary>
    /// Acquires a PostgreSQL advisory lock.
    /// </summary>
    private async Task<bool> TryAcquireAdvisoryLockAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Use a fixed lock key for migrations (based on "migration" string hash)
            const long lockKey = 1234567890;

            var result = await _context.Database.ExecuteSqlRawAsync(
                $"SELECT pg_try_advisory_lock({lockKey})",
                cancellationToken
            );

            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Advisory lock not available, falling back to table lock");
            return false;
        }
    }

    /// <summary>
    /// Releases the PostgreSQL advisory lock.
    /// </summary>
    private async Task ReleaseAdvisoryLockAsync()
    {
        try
        {
            const long lockKey = 1234567890;
            await _context.Database.ExecuteSqlRawAsync($"SELECT pg_advisory_unlock({lockKey})");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error releasing advisory lock");
        }
    }

    /// <summary>
    /// Acquires a table-based lock using a lock record.
    /// </summary>
    private async Task<bool> TryAcquireTableLockAsync(CancellationToken cancellationToken)
    {
        const string lockName = "migration_execution_lock";

        try
        {
            // Check if lock exists and is not expired
            var existingLock = await _context.MigrationLocks.FirstOrDefaultAsync(
                l => l.LockName == lockName,
                cancellationToken
            );

            if (existingLock != null)
            {
                // Check if lock has expired
                if (existingLock.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogDebug(
                        "Lock is held by another process until {ExpiresAt}",
                        existingLock.ExpiresAt
                    );
                    return false;
                }

                // Lock has expired, remove it
                _logger.LogWarning(
                    "Removing expired migration lock from {AcquiredAt}",
                    existingLock.AcquiredAt
                );
                _context.MigrationLocks.Remove(existingLock);
            }

            // Create new lock
            var newLock = new MigrationLock
            {
                LockName = lockName,
                LockId = _lockId,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_lockTimeout),
            };

            await _context.MigrationLocks.AddAsync(newLock, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(ex, "Could not acquire table lock - likely a concurrency conflict");
            return false;
        }
    }

    /// <summary>
    /// Releases the table-based lock.
    /// </summary>
    private async Task ReleaseTableLockAsync()
    {
        try
        {
            var lockRecord = await _context.MigrationLocks.FirstOrDefaultAsync(l =>
                l.LockId == _lockId
            );

            if (lockRecord != null)
            {
                _context.MigrationLocks.Remove(lockRecord);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error releasing table lock");
        }
    }

    /// <summary>
    /// Extends the lock timeout.
    /// </summary>
    public async Task ExtendLockAsync(TimeSpan extension)
    {
        if (!_isLocked)
        {
            return;
        }

        try
        {
            var lockRecord = await _context.MigrationLocks.FirstOrDefaultAsync(l =>
                l.LockId == _lockId
            );

            if (lockRecord != null)
            {
                lockRecord.ExpiresAt = lockRecord.ExpiresAt.Add(extension);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Lock extended to {ExpiresAt}", lockRecord.ExpiresAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending lock timeout");
        }
    }
}
