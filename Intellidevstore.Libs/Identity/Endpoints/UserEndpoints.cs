using Carter;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Identity.CQRS.Command;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Shared.Common;
using Intellidevstore.Libs.Shared.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Intellidevstore.Libs.Identity.Endpoints;

public sealed class UserEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        // -----------------------------
        // REGISTER
        // -----------------------------
        group
            .MapPost("/register", RegisterUserAsync)
            .Produces<User>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // -----------------------------
        // LOGIN
        // -----------------------------
        group
            .MapPost("/login", LoginAsync)
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status400BadRequest);

        // -----------------------------
        // LOGOUT
        // -----------------------------
        group
            .MapPost("/logout", LogoutAsync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest)
            .RequireAuthorization();

        // -----------------------------
        // REFRESH TOKEN
        // -----------------------------
        group
            .MapPost("/refresh-token", RefreshTokenAsync)
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status400BadRequest);
    }

    // =============================
    // Handlers
    // =============================

    private static async Task<IResult> RegisterUserAsync(
        [FromBody] CreateUserRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken ct
    )
    {
        var command = new CreateUserCommand(request, Guid.NewGuid());
        var result = await dispatcher.Send(command, ct);
        return result.IsFailure ? Results.BadRequest(result.Error) : Results.Ok(result.Value);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] IDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var deviceInfo = request.DeviceInfo ?? userAgent;

        // Get grant_type from header, default to "refresh_token"
        var grantType = httpContext.Request.Headers["grant_type"].ToString();
        if (string.IsNullOrEmpty(grantType))
        {
            grantType = "refresh_token";
        }

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

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest request,
        [FromServices] IDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        // Get user ID from claims
        var userIdClaim = httpContext.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        );
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Results.Unauthorized();
        }

        var command = new LogoutCommand(request, userId);
        var result = await dispatcher.Send(command, ct);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(new { message = "Logged out successfully" });
    }

    private static async Task<IResult> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IDispatcher dispatcher,
        CancellationToken ct
    )
    {
        var command = new RefreshTokenCommand(request);
        var result = await dispatcher.Send(command, ct);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.Unauthorized
                ? Results.Unauthorized()
                : Results.BadRequest(result.Error);
        }

        return Results.Ok(result.Value);
    }
}
