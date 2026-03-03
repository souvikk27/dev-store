using Carter;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.CQRS.Command;
using Intellidevstore.Libs.Messaging;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace intelli_dev_store.Api;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterUser);
        group.MapPost("/login", Login);
        group.MapPost("/logout", Logout);
        group.MapPost("/refresh-token", RefreshToken);
    }

    private static async Task<IResult> RegisterUser(
        [FromBody] CreateUserRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken ct
    )
    {
        var command = new CreateUserCommand(request, Guid.NewGuid());
        var result = await dispatcher.Send(command, ct);
        return result.IsFailure
            ? Results.BadRequest(result.Error)
            : Results.Created($"/api/v1/users/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var deviceInfo = request.DeviceInfo ?? userAgent;
        var grantType = httpContext.Request.Headers["grant_type"].ToString();
        if (string.IsNullOrEmpty(grantType))
            grantType = "refresh_token";

        var command = new LoginCommand(request, ipAddress, userAgent, deviceInfo, grantType);
        var result = await dispatcher.Send(command, ct);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Results.Unauthorized(),
                ErrorType.Forbidden => Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    detail: result.Error.Description
                ),
                _ => Results.BadRequest(result.Error),
            };
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] IDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var userIdClaim = httpContext.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        );
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Results.Unauthorized();

        var command = new LogoutCommand(request, userId);
        var result = await dispatcher.Send(command, ct);

        return result.IsFailure
            ? Results.BadRequest(result.Error)
            : Results.Ok(new { message = "Logged out successfully" });
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken ct
    )
    {
        var command = new RefreshTokenCommand(request);
        var result = await dispatcher.Send(command, ct);

        return result.IsFailure
            ? (
                result.Error.Type == ErrorType.Unauthorized
                    ? Results.Unauthorized()
                    : Results.BadRequest(result.Error)
            )
            : Results.Ok(result.Value);
    }
}
