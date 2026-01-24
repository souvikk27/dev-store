using Intellidevstore.Libs.Shared.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Intellidevstore.Libs.Database.Interceptors;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        if (eventData.Context is null)
            return base.SavingChanges(eventData, result);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable delete })
            {
                entry.State = EntityState.Modified;

                // Note: The actual Delete logic (setting IsDeleted, DeletedAt, etc.)
                // is typically handled by the entity's Delete() method called by the application layer.
                // However, if an entity is simply removed from a DbSet, EF Core marks it as Deleted.
                // This interceptor ensures that if we do context.Remove(entity), it becomes a soft delete operation.
                // We need to manually set the properties here if they aren't already set.

                if (!delete.IsDeleted)
                {
                    // If the entity wasn't already marked as deleted via its method
                    // (which would have set it to Modified), and is instead being Hard Deleted,
                    // we convert it to a Soft Delete here.

                    // We can't easily get the CurrentUserId here without injecting a service,
                    // but Interceptors are usually singletons or scoped differently.
                    // For now, we'll set the flag and timestamp.
                    // The 'DeletedBy' might be missing if we rely purely on context.Remove().

                    // Using reflection or a known interface method would be better if we want to set DeletedBy.
                    // But ISoftDeletable properties are read-only in the interface definition unless we cast to the concrete class
                    // or if the interface allowed setters (which it only defines getters for in the file I viewed).

                    // Looking at BaseEntity.cs, SoftDeletableEntity implementation has protected setters.

                    if (entry.Entity is SoftDeletableEntity softDeletable)
                    {
                        // We use a dummy ID or Guid.Empty if we don't have the user ID.
                        // Ideally, the app service calls entity.Delete(userId).
                        softDeletable.Delete(Guid.Empty);
                    }
                }
            }
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable delete })
            {
                entry.State = EntityState.Modified;

                if (!delete.IsDeleted)
                {
                    if (entry.Entity is SoftDeletableEntity softDeletable)
                    {
                        softDeletable.Delete(Guid.Empty);
                    }
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
