using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Entities;
using Microsoft.Extensions.Logging;

namespace Intellidevstore.Libs.Identity.Events;

public record LoginEvent(
    Guid UserId,
    string IpAddress,
    string UserAgent,
    string DeviceInfo,
    string GrantType,
    DateTime Timestamp
);

public class LoginEventHandler
{
    private readonly ILogger<LoginEventHandler> _logger;
    private readonly ApplicationDbContext _context;

    public LoginEventHandler(ILogger<LoginEventHandler> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task Handle(LoginEvent @event)
    {
        // Find user by event's UserId
        var sessionToken = Guid.NewGuid().ToString();
        var userSession = new UserSession(
            Guid.NewGuid(),
            @event.UserId,
            sessionToken,
            @event.DeviceInfo,
            @event.IpAddress,
            @event.UserAgent,
            @event.UserId
        );

        _context.UserSessions.Add(userSession);
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "User session created for user ID {UserId}: {SessionToken}",
            @event.UserId,
            sessionToken
        );
    }
}
