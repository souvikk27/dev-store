using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public string? SessionToken { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public bool IsEnded { get; set; }
    public string? ReasonForEnd { get; set; }

    // Navigation property
    public virtual User? User { get; set; }

    protected UserSession() { }

    public UserSession(
        Guid id,
        Guid userId,
        string sessionToken,
        string deviceInfo,
        string ipAddress,
        string userAgent,
        Guid createdBy
    )
        : base(id, createdBy)
    {
        UserId = userId;
        SessionToken = sessionToken;
        StartedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        DeviceInfo = deviceInfo;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsActive = true;
        IsEnded = false;
    }

    public void EndSession(string? reason = null)
    {
        EndedAt = DateTime.UtcNow;
        IsActive = false;
        IsEnded = true;
        ReasonForEnd = reason;
        SetModified(CreatedBy);
    }

    public void UpdateLastActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}
