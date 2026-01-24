using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class PlatformRefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Token { get; set; }
    public string? JwtId { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? ReasonForRevocation { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }

    // Navigation property
    public virtual User? User { get; set; }

    protected PlatformRefreshToken() { }

    public PlatformRefreshToken(
        Guid id,
        Guid userId,
        string token,
        DateTime expiryDate,
        Guid createdBy
    )
        : base(id, createdBy)
    {
        UserId = userId;
        Token = token;
        ExpiryDate = expiryDate;
        IsUsed = false;
        IsRevoked = false;
    }
}
