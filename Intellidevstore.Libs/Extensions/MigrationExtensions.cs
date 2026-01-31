using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Database.Migrations;
using Intellidevstore.Libs.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Extensions;

/// <summary>
/// Extension methods for automatic migration execution.
/// </summary>
public static class MigrationExtensions
{
    /// <summary>
    /// Executes automatic migrations at application startup with locking and duplicate prevention.
    /// </summary>
    public static async Task<IApplicationBuilder> UseAutoMigrationsAsync(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default
    )
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var logger = services.GetRequiredService<ILogger<AutoMigrationService>>();
        var lockLogger = services.GetRequiredService<ILogger<MigrationLockService>>();
        var seederLogger = services.GetRequiredService<ILogger<SuperAdminSeeder>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Ensure migration tracking tables exist
        await EnsureMigrationTablesAsync(context, logger);

        // Try to acquire migration lock
        var lockService = new MigrationLockService(context, lockLogger);
        var lockAcquired = await lockService.TryAcquireLockAsync(cancellationToken);

        if (!lockAcquired)
        {
            logger.LogWarning(
                "Could not acquire migration lock. Another instance may be running migrations."
            );
            logger.LogInformation("Waiting for migrations to complete...");

            // Wait for migrations to complete by polling
            await WaitForMigrationsToCompleteAsync(context, logger, cancellationToken);
            return app;
        }

        try
        {
            logger.LogInformation("Starting automatic migration execution...");

            // Create migration service and register migrations
            var migrationService = new AutoMigrationService(context, logger);
            RegisterMigrations(migrationService, services);

            // Execute migrations
            var result = await migrationService.ExecuteMigrationsAsync(
                Guid.Parse("00000000-0000-0000-0000-000000000001")
            );

            if (result.IsSuccess)
            {
                logger.LogInformation(
                    "Successfully executed {Count} migrations",
                    result.SuccessCount
                );
            }
            else
            {
                logger.LogError(
                    "Migration execution completed with {FailureCount} failures",
                    result.FailureCount
                );

                foreach (var failure in result.FailedMigrations)
                {
                    logger.LogError(
                        "Migration {MigrationId} failed: {ErrorMessage}",
                        failure.MigrationId,
                        failure.ErrorMessage
                    );
                }
            }

            // Seed super admin after migrations
            await SeedSuperAdminAsync(services, seederLogger);
        }
        finally
        {
            // Always release the lock
            await lockService.ReleaseLockAsync();
        }

        return app;
    }

    /// <summary>
    /// Synchronous version for use in non-async contexts.
    /// </summary>
    public static IApplicationBuilder UseAutoMigrations(this IApplicationBuilder app)
    {
        app.UseAutoMigrationsAsync().GetAwaiter().GetResult();
        return app;
    }

    /// <summary>
    /// Ensures migration tracking tables exist in the database.
    /// </summary>
    private static async Task EnsureMigrationTablesAsync(
        ApplicationDbContext context,
        ILogger logger
    )
    {
        try
        {
            logger.LogDebug("Ensuring migration tracking tables exist...");

            // Apply any pending EF Core migrations first
            await context.Database.MigrateAsync();

            logger.LogDebug("Migration tracking tables ensured");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure migration tables exist");
            throw;
        }
    }

    /// <summary>
    /// Waits for migrations to complete by polling the migration records.
    /// </summary>
    private static async Task WaitForMigrationsToCompleteAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken,
        int maxWaitSeconds = 300
    )
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(maxWaitSeconds))
        {
            // Check if migration lock still exists
            var lockExists = await context.MigrationLocks.AnyAsync(cancellationToken);

            if (!lockExists)
            {
                logger.LogInformation("Migration lock released. Proceeding with startup.");
                return;
            }

            logger.LogDebug("Migration lock still active. Waiting...");
            await Task.Delay(1000, cancellationToken);
        }

        logger.LogWarning("Timeout waiting for migrations to complete. Proceeding anyway.");
    }

    /// <summary>
    /// Registers all migrations to be executed.
    /// </summary>
    private static void RegisterMigrations(
        AutoMigrationService migrationService,
        IServiceProvider services
    )
    {
        // Register migrations here in order of execution
        // Example:
        // migrationService.RegisterMigration(new InitialDataMigration());
        // migrationService.RegisterMigration(new AddDefaultPermissionsMigration());

        // For now, we don't have specific migrations to register
        // The EF Core migrations are handled by EnsureMigrationTablesAsync
    }

    /// <summary>
    /// Seeds the super admin user if it doesn't exist.
    /// </summary>
    private static async Task SeedSuperAdminAsync(
        IServiceProvider services,
        ILogger<SuperAdminSeeder> logger
    )
    {
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = services.GetRequiredService<IPasswordHasherService>();

            var seeder = new SuperAdminSeeder(context, passwordHasher, logger);

            var options = new SuperAdminSeedOptions
            {
                UserName = "superadmin",
                Email = "superadmin@devstore.com",
                FirstName = "Super",
                LastName = "Admin",
                Password = "Admin@123!",
            };

            var result = await seeder.SeedAsync(options);

            if (result.IsSuccess)
            {
                if (result.UserAlreadyExists)
                {
                    logger.LogInformation(
                        "Super admin user already exists (ID: {UserId})",
                        result.UserId
                    );
                }
                else
                {
                    logger.LogInformation(
                        "Super admin user created successfully (ID: {UserId})",
                        result.UserId
                    );
                    logger.LogWarning(
                        "Default super admin credentials: Username='{UserName}', Password='{Password}'. "
                            + "Please change the password after first login!",
                        options.UserName,
                        options.Password
                    );
                }
            }
            else
            {
                logger.LogError(
                    "Failed to seed super admin user: {ErrorMessage}",
                    result.ErrorMessage
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding super admin user");
        }
    }
}
