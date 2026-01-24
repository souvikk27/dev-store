using Intellidevstore.Libs.Shared.Entity;

namespace Intellidevstore.Libs.Identity.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
    
    public DateTime AssignedDate { get; set; }
    public Guid AssignedBy { get; set; }
    public string? Notes { get; set; }
    
    protected UserRole() { }

    public UserRole(Guid userId, Guid roleId, Guid assignedBy) : base()
    {
        UserId = userId;
        RoleId = roleId;
        AssignedBy = assignedBy;
        AssignedDate = DateTime.UtcNow;
    }
}