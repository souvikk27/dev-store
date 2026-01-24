using Carter;
using Intellidevstore.Libs;

namespace intelli_dev_store.Extensions;

public static class ServiceExtensions
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add application services here
        services.ConfigureClassLibrary(configuration);
        services.AddCarter();
    }
}