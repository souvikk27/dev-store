using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Contracts;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record LogoutCommand(LogoutRequest Request, Guid UserId);

public sealed class LogoutHandler
{
    private readonly ApplicationDbContext _context;

    public LogoutHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(LogoutCommand command)
    {
        if (command.Request.LogoutAllDevices)
        {
            // Revoke all refresh tokens for the user
            var allRefreshTokens = await _context
                .PlatformRefreshTokens.Where(rt =>
                    rt.UserId == command.UserId && !rt.IsRevoked && !rt.IsUsed
                )
                .ToListAsync();

            foreach (var token in allRefreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedDate = DateTime.UtcNow;
                token.ReasonForRevocation = "User logged out from all devices";
                token.SetModified(command.UserId);
            }

            // End all active sessions
            var allSessions = await _context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .ToListAsync();

            foreach (var session in allSessions)
            {
                session.EndSession("User logged out from all devices");
            }
        }
        else if (!string.IsNullOrEmpty(command.Request.RefreshToken))
        {
            // Revoke specific refresh token
            var refreshToken = await _context.PlatformRefreshTokens.FirstOrDefaultAsync(rt =>
                rt.Token == command.Request.RefreshToken && rt.UserId == command.UserId
            );

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedDate = DateTime.UtcNow;
                refreshToken.ReasonForRevocation = "User logged out";
                refreshToken.SetModified(command.UserId);
            }

            // End the associated session (find by JWT ID or most recent active session)
            var activeSession = await _context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();

            if (activeSession != null)
            {
                activeSession.EndSession("User logged out");
            }
        }
        else
        {
            // If no refresh token provided, end the most recent active session
            var activeSession = await _context
                .UserSessions.Where(s => s.UserId == command.UserId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .FirstOrDefaultAsync();

            if (activeSession != null)
            {
                activeSession.EndSession("User logged out");
            }
        }

        await _context.SaveChangesAsync();

        return Result.Success();
    }
}
