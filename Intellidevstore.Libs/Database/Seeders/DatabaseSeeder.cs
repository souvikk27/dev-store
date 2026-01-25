using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Seeders;

public class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAllAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            var roleSeeder = new RoleSeeder(
                context,
                scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>()
            );
            await roleSeeder.SeedAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
