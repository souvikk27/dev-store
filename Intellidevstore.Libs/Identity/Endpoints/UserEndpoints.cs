using Intellidevstore.Libs.Identity.CQRS.Command;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace Intellidevstore.Libs.Identity.Endpoints;

public static class UserEndpoints
{
    [WolverinePost("/api/v1/auth/register")]
    public static async Task<IResult> RegisterUser(
        CreateUserRequest request,
        IMessageBus bus,
        HttpContext httpContext
    )
    {
        var command = new CreateUserCommand(request, Guid.NewGuid());
        var result = await bus.InvokeAsync<Result<User>>(command);
        return result.IsFailure ? Results.BadRequest(result.Error) : Results.Ok(result.Value);
    }
}
