using System.Text;
using Carter;
using intelli_dev_store.Authentication;
using Intellidevstore.Libs;
using Intellidevstore.Libs.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

    public static void ConfigureAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var jwtSettings =
            configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured");

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Smart";
                options.DefaultChallengeScheme = "Smart";
                options.DefaultAuthenticateScheme = "Smart";
            })
            .AddPolicyScheme(
                "Smart",
                "JWT or API Key",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        // API Key has priority
                        if (context.Request.Headers.ContainsKey("X-API-KEY"))
                            return "ApiKey";

                        if (context.Request.Headers.ContainsKey("Authorization"))
                            return "Bearer";

                        // Default to Bearer for unauthenticated requests
                        return "Bearer";
                    };
                }
            )
            .AddJwtBearer(
                "Bearer",
                options =>
                {
                    options.Authority = jwtSettings.Authority;
                    options.Audience = jwtSettings.Audience;

                    // Allow requests without authentication (will be handled by endpoints)
                    options.RequireHttpsMetadata = false;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                        ),
                        ClockSkew = TimeSpan.Zero,
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (
                                context.Exception.GetType() == typeof(SecurityTokenExpiredException)
                            )
                            {
                                context.Response.Headers.Append("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            // Allow requests without Authorization header to pass through
                            if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
                            {
                                context.NoResult();
                            }
                            return Task.CompletedTask;
                        },
                    };
                }
            )
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                "ApiKey",
                _ => { }
            );

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKeyOnly", p => p.RequireClaim("auth_type", "api_key"));
        });
    }
}
