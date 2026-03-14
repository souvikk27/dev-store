using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.CQRS.Command;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Messaging;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Intellidevstore.Libs.Identity.Extensions;

public static class IdentityExtensions
{
    public static void AddIdentityLibs(this IServiceCollection services)
    {
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
