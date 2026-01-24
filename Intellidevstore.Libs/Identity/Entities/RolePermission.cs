using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    
    // Navigation properties
    public virtual Role? Role { get; set; }
    public virtual Permission? Permission { get; set; }
    
    public DateTime GrantedDate { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    
    protected RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId, Guid grantedBy) : base()
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedBy = grantedBy;
        GrantedDate = DateTime.UtcNow;
        IsActive = true;
    }
}