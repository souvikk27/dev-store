using Carter;
using Intellidevstore.Libs;
using Intellidevstore.Libs.Extensions;
using Serilog;
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
            options.Discovery.IncludeAssembly(typeof(IntelliDevStoreLibAssemblyMarker).Assembly);
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

    public static void ConfigureBootstrapLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile(
                        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                        optional: true
                    )
                    .Build()
            )
            .CreateBootstrapLogger();
    }

    public static IHostBuilder UseSerilogLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog(
            (context, services, configuration) =>
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
        );
    }

    public static IApplicationBuilder UseSerilogRequestLoggingMiddleware(
        this IApplicationBuilder app
    )
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                diagnosticContext.Set(
                    "UserAgent",
                    httpContext.Request.Headers.UserAgent.ToString()
                );
            };
        });
    }
}
