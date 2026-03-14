using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Database.Interceptors;
using Intellidevstore.Libs.Identity;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.CQRS.Command;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Messaging;
using Intellidevstore.Libs.Shared.Common;
using Intellidevstore.Libs.Shared.Services;
using Intellidevstore.Libs.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpGrip.FileSystem;
using SharpGrip.FileSystem.Adapters;

namespace Intellidevstore.Libs;

public static class DependencyInjection
{
    public static void ConfigureClassLibrary(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHttpContextAccessor();

        // Register CurrentUserService
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddSingleton<IFileSystem>(sp =>
        {
            // Try to get the content root path from IHostEnvironment
            var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            var rootPath = hostEnvironment.ContentRootPath;
            
            // Fallback: if we can't get it from host environment, calculate it
            if (string.IsNullOrEmpty(rootPath))
            {
                var basePath = AppContext.BaseDirectory;
                // Navigate from bin/Debug/net10.0 back to project root
                rootPath = Directory.GetParent(basePath)?.Parent?.Parent?.FullName 
                    ?? Directory.GetCurrentDirectory();
            }
            
            var wwwrootPath = Path.Combine(rootPath, "wwwroot");

            // Ensure the root directory exists for the LocalAdapter
            if (!Directory.Exists(wwwrootPath))
            {
                Directory.CreateDirectory(wwwrootPath);
            }

            var adapters = new List<IAdapter> { new LocalAdapter("app", wwwrootPath) };

            return new FileSystem(adapters);
        });

        services.AddSingleton<IFileStorage>(sp => new LocalFileStorage(
            sp.GetRequiredService<IFileSystem>(),
            "app"
        ));

        services.AddSingleton<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>(
            (sp, options) =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Database"));
                options.UseSnakeCaseNamingConvention();
                options.AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
            }
        );

        services.AddIdentityServices();
        services.AddLightweightCqrs();
        services.AddCommandHandler<CreateUserCommand, Result<User>, CreateUserHandler>();
        services.AddCommandHandler<LoginCommand, Result<object>, LoginHandler>();
        services.AddCommandHandler<LogoutCommand, Result, LogoutHandler>();
        services.AddCommandHandler<
            RefreshTokenCommand,
            Result<RefreshTokenResponse>,
            RefreshTokenHandler
        >();
    }
}
