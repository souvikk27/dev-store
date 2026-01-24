namespace Intellidevstore.Libs.Shared.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// The tenant context in which the event occurred.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// The user who triggered the event.
    /// </summary>
    Guid TriggeredBy { get; }
}

/// <summary>
/// Marker interface for integration events.
/// Integration events are published to external systems via message broker.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    Guid TenantId { get; }
    string EventType { get; }
}

/// <summary>
/// Base record for domain events.
/// Uses record for immutability and value equality.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public Guid TriggeredBy { get; init; }

    protected DomainEventBase(Guid tenantId, Guid triggeredBy)
    {
        TenantId = tenantId;
        TriggeredBy = triggeredBy;
    }
}

/// <summary>
/// Base record for integration events.
/// These are serialized and sent via RabbitMQ.
/// </summary>
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public string EventType => GetType().Name;

    protected IntegrationEventBase(Guid tenantId)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when an entity is created.
/// </summary>
public abstract record EntityCreatedEvent<TEntity> : DomainEventBase
    where TEntity : class
{
    public Guid EntityId { get; }
    public string EntityType => typeof(TEntity).Name;

    protected EntityCreatedEvent(Guid entityId, Guid tenantId, Guid triggeredBy)
        : base(tenantId, triggeredBy)
    {
        EntityId = entityId;
    }
}

/// <summary>
/// Domain event raised when an entity is updated.
/// </summary>
public abstract record EntityUpdatedEvent<TEntity> : DomainEventBase
    where TEntity : class
{
    public Guid EntityId { get; }
    public string EntityType => typeof(TEntity).Name;
    public IReadOnlyDictionary<string, object?> ChangedProperties { get; }

    protected EntityUpdatedEvent(
        Guid entityId,
        IReadOnlyDictionary<string, object?> changedProperties,
        Guid tenantId,
        Guid triggeredBy
    )
        : base(tenantId, triggeredBy)
    {
        EntityId = entityId;
        ChangedProperties = changedProperties;
    }
}

/// <summary>
/// Domain event raised when an entity is deleted.
/// </summary>
public abstract record EntityDeletedEvent<TEntity> : DomainEventBase
    where TEntity : class
{
    public Guid EntityId { get; }
    public string EntityType => typeof(TEntity).Name;
    public bool IsSoftDelete { get; }

    protected EntityDeletedEvent(Guid entityId, bool isSoftDelete, Guid tenantId, Guid triggeredBy)
        : base(tenantId, triggeredBy)
    {
        EntityId = entityId;
        IsSoftDelete = isSoftDelete;
    }
}
