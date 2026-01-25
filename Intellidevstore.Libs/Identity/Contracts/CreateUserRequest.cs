using Intellidevstore.Libs.Identity.Entities;

namespace Intellidevstore.Libs.Identity.CQRS.Command;

public record CreateUserRequest(
    string UserName,
    string Email,
    string Password,
    string FirstName,
    string LastName
);
