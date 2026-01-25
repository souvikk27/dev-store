using Intellidevstore.Libs.Identity.Contracts;
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

    [WolverinePost("/api/v1/auth/login")]
    public static async Task<IResult> Login(
        LoginRequest request,
        IMessageBus bus,
        HttpContext httpContext
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
        var result = await bus.InvokeAsync<Result<object>>(command);

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

    [WolverinePost("/api/v1/auth/logout")]
    public static async Task<IResult> Logout(
        LogoutRequest request,
        IMessageBus bus,
        HttpContext httpContext
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
        var result = await bus.InvokeAsync<Result>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(new { message = "Logged out successfully" });
    }

    [WolverinePost("/api/v1/auth/refresh-token")]
    public static async Task<IResult> RefreshToken(
        RefreshTokenRequest request,
        IMessageBus bus,
        HttpContext httpContext
    )
    {
        var command = new RefreshTokenCommand(request);
        var result = await bus.InvokeAsync<Result<RefreshTokenResponse>>(command);

        if (result.IsFailure)
        {
            return result.Error.Type == ErrorType.Unauthorized
                ? Results.Unauthorized()
                : Results.BadRequest(result.Error);
        }

        return Results.Ok(result.Value);
    }
}
