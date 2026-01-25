using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Database.Seeders;

public static class DatabaseSeederExtensions
{
    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
            var seeder = new DatabaseSeeder(services, logger);
            await seeder.SeedAllAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    /// <summary>
    /// Seeds the database with initial data (synchronous version).
    /// </summary>
    public static void SeedDatabase(this IApplicationBuilder app)
    {
        app.SeedDatabaseAsync().GetAwaiter().GetResult();
    }
}
