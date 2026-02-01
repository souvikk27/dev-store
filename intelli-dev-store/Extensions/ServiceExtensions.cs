using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Carter;
using intelli_dev_store.Authentication;
using Intellidevstore.Libs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

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
