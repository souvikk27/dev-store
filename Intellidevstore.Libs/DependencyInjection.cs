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

        services.AddSingleton<IFileSystem>(_ =>
        {
            var rootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");

            // Ensure the root directory exists for the LocalAdapter
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            var adapters = new List<IAdapter> { new LocalAdapter("app", rootPath) };

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
