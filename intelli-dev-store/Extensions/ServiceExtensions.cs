using Carter;
using Intellidevstore.Libs;
using Intellidevstore.Libs.Extensions;
using Wolverine;

namespace intelli_dev_store.Extensions;

public static class ServiceExtensions
{
    public static void AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Add application services here
        services.ConfigureClassLibrary(configuration);
        services.AddCarter();
    }

    public static WebApplicationBuilder AddWolverineWithRabbitMq(this WebApplicationBuilder builder)
    {
        var connectionString =
            builder.Configuration.GetConnectionString("RabbitMQ")
            ?? throw new InvalidOperationException("RabbitMQ connection string missing");

        builder.Host.UseWolverine(options =>
        {
            options.Discovery.IncludeAssembly(
                typeof(Intellidevstore.Libs.IntelliDevStoreLibAssemblyMarker).Assembly
            );
            options.ConfigureWolverine(connectionString);
            options.UseSystemTextJsonForSerialization(jsonOptions =>
            {
                jsonOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                jsonOptions.DefaultIgnoreCondition = System
                    .Text
                    .Json
                    .Serialization
                    .JsonIgnoreCondition
                    .WhenWritingNull;
                jsonOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                );
            });
        });

        return builder;
    }
}
