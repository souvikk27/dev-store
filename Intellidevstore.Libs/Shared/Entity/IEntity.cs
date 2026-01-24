namespace Intellidevstore.Libs.Shared.Entity;

/// <summary>
/// Marker interface for all domain entities.
/// </summary>
public interface IEntity;

/// <summary>
/// Base interface for entities with a typed identifier.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IEntity<out TId> : IEntity
    where TId : notnull
{
    TId Id { get; }
}

/// <summary>
/// Interface for tenant-scoped entities. All tenant data MUST implement this.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant identifier. Maps to the Institute/College in the ERP.
    /// </summary>
    Guid TenantId { get; }
}

/// <summary>
/// Interface for auditable entities. Required for NAAC evidence trails.
/// Every change must be traceable for compliance audits.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the record was created (UTC).
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// User ID who created the record.
    /// </summary>
    Guid CreatedBy { get; }

    /// <summary>
    /// When the record was last modified (UTC).
    /// </summary>
    DateTime? ModifiedAt { get; }

    /// <summary>
    /// User ID who last modified the record.
    /// </summary>
    Guid? ModifiedBy { get; }

    /// <summary>
    /// Reference to the audit log entry for this change.
    /// Links to immutable audit trail for NAAC DVV.
    /// </summary>
    Guid? AuditReferenceId { get; }
}

/// <summary>
/// Interface for soft-deletable entities.
/// Used where hard delete is not allowed (exam records, financial data).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid? DeletedBy { get; }
}

/// <summary>
/// Interface for entities that support versioning.
/// Critical for NAAC - curriculum changes, regulations, etc.
/// </summary>
public interface IVersionedEntity
{
    int Version { get; }
    bool IsActive { get; }
    DateTime? EffectiveFrom { get; }
    DateTime? EffectiveTo { get; }
}
