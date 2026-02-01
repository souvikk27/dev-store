using Intellidevstore.Libs.Database;
using Intellidevstore.Libs.Identity.Entities;
using Intellidevstore.Libs.Identity.Services;
using Intellidevstore.Libs.Shared.Common;
using Intellidevstore.Libs.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record CreateUserCommand(CreateUserRequest Request, Guid CreatedBy) : ICommand<Result<User>>;

public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Result<User>>
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasherService _passwordHasherService;

    public CreateUserHandler(
        ApplicationDbContext context,
        IPasswordHasherService passwordHasherService
    )
    {
        _context = context;
        _passwordHasherService = passwordHasherService;
    }

    public async Task<Result<User>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken = default
    )
    {
        // Check if user with same email or username already exists
        var existingUser = await _context
            .Users.Where(u =>
                u.Email!.ToLower() == command.Request.Email.ToLower()
                || u.UserName!.ToLower() == command.Request.UserName.ToLower()
            )
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            var conflictError = Error.Conflict(
                "User.AlreadyExists",
                "A user with the same email or username already exists."
            );
            return Result.Failure<User>(conflictError);
        }

        // Hash the password
        var passwordHash = _passwordHasherService.HashPassword(command.Request.Password);

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
        _context.Users.Add(user);

        // Save changes
        await _context.SaveChangesAsync();

        return Result.Success(user);
    }
}
