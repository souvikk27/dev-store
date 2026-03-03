using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Messaging.Command;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record LogoutCommand(LogoutRequest Request, Guid UserId) : ICommand<Result>;

public sealed class LogoutHandler(ApplicationDbContext context)
    : ICommandHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand command, CancellationToken ct = default)
    {
        if (command.Request.LogoutAllDevices)
        {
            // Revoke all refresh tokens for the user
            var allRefreshTokens = await context
                .PlatformRefreshTokens.Where(rt =>
                    rt.UserId == command.UserId && !rt.IsRevoked && !rt.IsUsed
                )
                .ToListAsync(cancellationToken: ct);

            foreach (var token in allRefreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedDate = DateTime.UtcNow;
                token.ReasonForRevocation = "User logged out from all devices";
                token.SetModified(command.UserId);
            }

            // End all active sessions
            var allSessions = await context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .ToListAsync(cancellationToken: ct);

            foreach (var session in allSessions)
            {
                session.EndSession("User logged out from all devices");
            }
        }
        else if (!string.IsNullOrEmpty(command.Request.RefreshToken))
        {
            // Revoke specific refresh token
            var refreshToken = await context.PlatformRefreshTokens.FirstOrDefaultAsync(
                rt => rt.Token == command.Request.RefreshToken && rt.UserId == command.UserId,
                cancellationToken: ct
            );

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedDate = DateTime.UtcNow;
                refreshToken.ReasonForRevocation = "User logged out";
                refreshToken.SetModified(command.UserId);
            }

            // End the associated session (find by JWT ID or most recent active session)
            var activeSession = await context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync(cancellationToken: ct);

            activeSession?.EndSession("User logged out");
        }
        else
        {
            // If no refresh token provided, end the most recent active session
            var activeSession = await context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync(cancellationToken: ct);

            activeSession?.EndSession("User logged out");
        }

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
