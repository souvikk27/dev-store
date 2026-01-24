using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class UserPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Permission? Permission { get; set; }
    
    public DateTime GrantedDate { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    
    protected UserPermission() { }

    public UserPermission(Guid userId, Guid permissionId, Guid grantedBy) : base()
    {
        UserId = userId;
        PermissionId = permissionId;
        GrantedBy = grantedBy;
        GrantedDate = DateTime.UtcNow;
        IsActive = true;
    }
}