using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Identity.Services;
using Intellidevstore.Libs.Messaging.Command;
using Intellidevstore.Libs.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record CreateUserCommand(CreateUserRequest Request, Guid CreatedBy) : ICommand<Result<User>>;

public sealed class CreateUserHandler(
    ApplicationDbContext context,
    IPasswordHasherService passwordHasherService)
    : ICommandHandler<CreateUserCommand, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserCommand command, CancellationToken ct = default)
    {
        // Check if user with same email or username already exists
        var existingUser = await context
            .Users.Where(u =>
                u.Email != null && u.UserName != null &&
                (u.Email.ToLower() == command.Request.Email.ToLower()
                 || u.UserName.ToLower() == command.Request.UserName.ToLower())
            )
            .FirstOrDefaultAsync(cancellationToken: ct);

        if (existingUser != null)
        {
            var conflictError = Error.Conflict(
                "User.AlreadyExists",
                "A user with the same email or username already exists."
            );
            return Result.Failure<User>(conflictError);
        }

        // Hash the password
        var passwordHash = passwordHasherService.HashPassword(command.Request.Password);

        // Create new user
        var user = new User(Guid.NewGuid(), command.CreatedBy)
        {
            UserName = command.Request.UserName,
            Email = command.Request.Email,
            PasswordHash = passwordHash,
            FirstName = command.Request.FirstName,
            LastName = command.Request.LastName,
            IsActive = true,
            EmailConfirmed = false,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = command.CreatedBy,
        };

        // Add user to context
        context.Users.Add(user);

        // Save changes
        await context.SaveChangesAsync(ct);

        return Result.Success(user);
    }
}
