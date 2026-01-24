using Intellidevstore.Libs.Shared.Events;

namespace Intellidevstore.Libs.Shared.Entity;

public abstract class BaseEntity : IEntity<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public Guid Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public Guid? ModifiedBy { get; set; }

    /// <summary>
    /// Domain events raised by this entity (not persisted).
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    public uint RowVersion { get; protected set; }

    protected BaseEntity() { }

    protected BaseEntity(Guid id, Guid createdBy)
    {
        Id = id;
        CreatedBy = createdBy;
        CreatedDate = DateTime.UtcNow;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void SetCreated(Guid createdBy)
    {
        CreatedBy = createdBy;
        CreatedDate = DateTime.UtcNow;
    }

    public void SetModified(Guid modifiedBy)
    {
        ModifiedBy = modifiedBy;
        ModifiedDate = DateTime.UtcNow;
    }
}

public abstract class SoftDeletableEntity : BaseEntity, ISoftDeletable
{
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    protected SoftDeletableEntity() { }

    protected SoftDeletableEntity(Guid id, Guid createdBy)
        : base(id, createdBy) { }

    /// <summary>
    /// Soft deletes the entity with audit trail.
    /// </summary>
    public virtual void Delete(Guid deletedBy)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public virtual void Restore(Guid restoredBy)
    {
        if (!IsDeleted)
            return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        SetModified(restoredBy);
    }
}
