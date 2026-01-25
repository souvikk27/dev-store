using Intellidevstore.Libs.Identity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Intellidevstore.Libs.Identity;

public static class IdentityDependencyInjection
{
    public static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
    }
}
