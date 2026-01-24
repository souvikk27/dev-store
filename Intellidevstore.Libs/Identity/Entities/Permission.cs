using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class Permission : SoftDeletableEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Resource { get; set; }
    public string? Action { get; set; }
    
    // Navigation properties
    public virtual ICollection<RolePermission>? RolePermissions { get; set; }
    public virtual ICollection<UserPermission>? UserPermissions { get; set; }
    
    protected Permission() { }

    public Permission(Guid id, Guid createdBy) : base(id, createdBy)
    {
        // Initialize permission-specific properties as needed
    }
}