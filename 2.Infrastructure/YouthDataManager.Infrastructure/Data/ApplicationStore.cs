using System.Data;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Data;

public sealed class ApplicationStore(AppDbContext context) : IApplicationStore
{
    public IQueryable<T> Query<T>(bool tracking = true, bool includeArchived = false) where T : class
    {
        IQueryable<T> query = context.Set<T>();
        if (!tracking) query = query.AsNoTracking();
        return includeArchived ? query.IgnoreQueryFilters().ForServiceYear(context) : query;
    }
    public void Add<T>(T entity) where T : class => context.Add(entity);
    public void Remove<T>(T entity) where T : class => context.Remove(entity);
    public void ClearTracking() => context.ChangeTracker.Clear();
    public async Task LockAdministration()
    {
        if(context.Database.CurrentTransaction==null)throw new InvalidOperationException("Administration lock requires an operation transaction.");
        await context.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'YDM.IdentityAdministration', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51001, 'Administration operation is busy. Retry.', 1;",context.RequestCancellation);
    }
    public async Task Save(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken==default ? context.RequestCancellation : cancellationToken);
    public async Task<T> Transaction<T>(Func<Task<T>> action, Func<T,bool>? succeeded = null)
    {
        var existing = context.Database.CurrentTransaction;
        var savepoint = Guid.NewGuid().ToString("N");
        var ct=context.RequestCancellation;
        ct.ThrowIfCancellationRequested();
        await using var transaction = existing == null ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct) : null;
        if (existing != null) await existing.CreateSavepointAsync(savepoint,ct);
        try
        {
            var result = await action();
            if (succeeded != null && !succeeded(result))
            {
                if (existing != null) await existing.RollbackToSavepointAsync(savepoint);
                else await transaction!.RollbackAsync();
                context.ChangeTracker.Clear();
                return result;
            }
            await context.SaveChangesAsync(ct);
            if (transaction != null) await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            if (existing != null) await existing.RollbackToSavepointAsync(savepoint);
            else await transaction!.RollbackAsync();
            context.ChangeTracker.Clear();
            throw;
        }
    }
}
