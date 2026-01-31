using System.Security.Cryptography;
using System.Text;
using Intellidevstore.Libs.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Migrations;

/// <summary>
/// Service responsible for automatic migration execution with duplicate prevention.
/// </summary>
public class AutoMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AutoMigrationService> _logger;
    private readonly List<IMigration> _migrations;

    public AutoMigrationService(ApplicationDbContext context, ILogger<AutoMigrationService> logger)
    {
        _context = context;
        _logger = logger;
        _migrations = new List<IMigration>();
    }

    /// <summary>
    /// Registers a migration to be executed.
    /// </summary>
    public void RegisterMigration(IMigration migration)
    {
        _migrations.Add(migration);
    }

    /// <summary>
    /// Detects and returns pending migrations that haven't been applied yet.
    /// </summary>
    public async Task<List<IMigration>> DetectPendingMigrationsAsync()
    {
        _logger.LogInformation("Detecting pending migrations...");

        var appliedMigrationIds = await _context
            .MigrationRecords.Where(m => m.IsSuccess)
            .Select(m => m.MigrationId)
            .ToListAsync();

        var pendingMigrations = _migrations
            .Where(m => !appliedMigrationIds.Contains(m.MigrationId))
            .OrderBy(m => m.MigrationId)
            .ToList();

        _logger.LogInformation(
            "Found {PendingCount} pending migrations out of {TotalCount} total migrations",
            pendingMigrations.Count,
            _migrations.Count
        );

        return pendingMigrations;
    }

    /// <summary>
    /// Checks if a specific migration has already been applied.
    /// </summary>
    public async Task<bool> IsMigrationAppliedAsync(string migrationId)
    {
        return await _context.MigrationRecords.AnyAsync(m =>
            m.MigrationId == migrationId && m.IsSuccess
        );
    }

    /// <summary>
    /// Executes all pending migrations in a transaction.
    /// </summary>
    public async Task<MigrationResult> ExecuteMigrationsAsync(Guid executedBy)
    {
        _logger.LogInformation("Starting automatic migration execution...");

        var pendingMigrations = await DetectPendingMigrationsAsync();

        if (pendingMigrations.Count == 0)
        {
            _logger.LogInformation("No pending migrations to execute");
            return MigrationResult.Success(0);
        }

        var result = new MigrationResult();
        var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Use execution strategy for resilience
        var executionStrategy = _context.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var migration in pendingMigrations)
                {
                    var migrationResult = await ExecuteSingleMigrationAsync(
                        migration,
                        systemUserId
                    );

                    if (migrationResult.IsSuccess)
                    {
                        result.AddSuccess(migration.MigrationId);
                        _logger.LogInformation(
                            "Migration {MigrationId} executed successfully in {ExecutionTimeMs}ms",
                            migration.MigrationId,
                            migrationResult.ExecutionTimeMs
                        );
                    }
                    else
                    {
                        result.AddFailure(migration.MigrationId, migrationResult.ErrorMessage!);
                        _logger.LogError(
                            "Migration {MigrationId} failed: {ErrorMessage}",
                            migration.MigrationId,
                            migrationResult.ErrorMessage
                        );

                        // Rollback transaction on failure
                        await transaction.RollbackAsync();
                        return;
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Migration execution failed with exception");
                throw;
            }
        });

        _logger.LogInformation(
            "Migration execution completed. Success: {SuccessCount}, Failed: {FailureCount}",
            result.SuccessCount,
            result.FailureCount
        );

        return result;
    }

    /// <summary>
    /// Executes a single migration and records the result.
    /// </summary>
    private async Task<SingleMigrationResult> ExecuteSingleMigrationAsync(
        IMigration migration,
        Guid executedBy
    )
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var migrationId = migration.MigrationId;

        try
        {
            _logger.LogDebug("Executing migration: {MigrationId}", migrationId);

            // Execute the migration
            await migration.UpAsync(_context);

            stopwatch.Stop();

            // Record successful migration
            var record = new MigrationRecord(Guid.NewGuid(), executedBy)
            {
                MigrationId = migrationId,
                Description = migration.Description,
                AppliedAt = DateTime.UtcNow,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = true,
                Checksum = ComputeChecksum(migration.GetType().FullName!),
            };

            await _context.MigrationRecords.AddAsync(record);
            await _context.SaveChangesAsync();

            return SingleMigrationResult.Success(stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record failed migration
            var record = new MigrationRecord(Guid.NewGuid(), executedBy)
            {
                MigrationId = migrationId,
                Description = migration.Description,
                AppliedAt = DateTime.UtcNow,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Checksum = ComputeChecksum(migration.GetType().FullName!),
            };

            await _context.MigrationRecords.AddAsync(record);
            await _context.SaveChangesAsync();

            return SingleMigrationResult.Failure(ex.Message, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Rolls back the last applied migration.
    /// </summary>
    public async Task<bool> RollbackLastMigrationAsync(Guid executedBy)
    {
        var lastMigration = await _context
            .MigrationRecords.Where(m => m.IsSuccess)
            .OrderByDescending(m => m.AppliedAt)
            .FirstOrDefaultAsync();

        if (lastMigration == null)
        {
            _logger.LogWarning("No migrations to rollback");
            return false;
        }

        _logger.LogInformation("Rolling back migration: {MigrationId}", lastMigration.MigrationId);

        // Find the migration implementation
        var migration = _migrations.FirstOrDefault(m => m.MigrationId == lastMigration.MigrationId);

        if (migration == null)
        {
            _logger.LogError(
                "Cannot rollback migration {MigrationId}: implementation not found",
                lastMigration.MigrationId
            );
            return false;
        }

        try
        {
            await migration.DownAsync(_context);

            // Remove the migration record
            _context.MigrationRecords.Remove(lastMigration);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully rolled back migration: {MigrationId}",
                lastMigration.MigrationId
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to rollback migration {MigrationId}",
                lastMigration.MigrationId
            );
            return false;
        }
    }

    /// <summary>
    /// Computes a checksum for migration integrity verification.
    /// </summary>
    private static string ComputeChecksum(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

/// <summary>
/// Interface for database migrations.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Unique identifier for the migration.
    /// </summary>
    string MigrationId { get; }

    /// <summary>
    /// Human-readable description of the migration.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Applies the migration.
    /// </summary>
    Task UpAsync(ApplicationDbContext context);

    /// <summary>
    /// Reverts the migration.
    /// </summary>
    Task DownAsync(ApplicationDbContext context);
}

/// <summary>
/// Result of executing all pending migrations.
/// </summary>
public class MigrationResult
{
    public List<string> SuccessfulMigrations { get; } = new();
    public List<MigrationFailure> FailedMigrations { get; } = new();
    public int SuccessCount => SuccessfulMigrations.Count;
    public int FailureCount => FailedMigrations.Count;
    public bool IsSuccess => FailureCount == 0;

    public void AddSuccess(string migrationId)
    {
        SuccessfulMigrations.Add(migrationId);
    }

    public void AddFailure(string migrationId, string errorMessage)
    {
        FailedMigrations.Add(new MigrationFailure(migrationId, errorMessage));
    }

    public static MigrationResult Success(int count)
    {
        return new MigrationResult();
    }
}

/// <summary>
/// Represents a failed migration.
/// </summary>
public record MigrationFailure(string MigrationId, string ErrorMessage);

/// <summary>
/// Result of executing a single migration.
/// </summary>
public class SingleMigrationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }

    public static SingleMigrationResult Success(long executionTimeMs)
    {
        return new SingleMigrationResult { IsSuccess = true, ExecutionTimeMs = executionTimeMs };
    }

    public static SingleMigrationResult Failure(string errorMessage, long executionTimeMs)
    {
        return new SingleMigrationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = executionTimeMs,
        };
    }
}
