using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class Role : SoftDeletableEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? NormalizedName { get; set; }
    
    // Navigation property for users with this role
    public virtual ICollection<UserRole>? UserRoles { get; set; }
    
    protected Role() { }

    public Role(Guid id, Guid createdBy) : base(id, createdBy)
    {
        // Initialize role-specific properties as needed
    }
}