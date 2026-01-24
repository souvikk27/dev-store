using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class User : SoftDeletableEntity
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PasswordHash { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresMfa { get; set; }
    public string? MfaSecret { get; set; }
    public bool MfaEnabled { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndAt { get; set; }

    // Additional user-specific properties can be added here

    protected User() { }

    public User(Guid id, Guid createdBy)
        : base(id, createdBy)
    {
        // Initialize user-specific properties as needed
    }
}
