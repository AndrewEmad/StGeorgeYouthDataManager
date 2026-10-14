namespace YouthDataManager.Shared.Service.Abstractions;

public interface IApplicationStore
{
    IQueryable<T> Query<T>(bool tracking = true, bool includeArchived = false) where T : class;
    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    void ClearTracking();
    Task LockAdministration();
    Task Save(CancellationToken cancellationToken = default);
    Task<T> Transaction<T>(Func<Task<T>> action, Func<T,bool>? succeeded = null);
}
public interface ICallerContext
{
    ServiceYear? SelectedServiceYear => null;
    bool AllServiceYears => false;
    string? ExpectedVersion => null;
    DateTime UtcNow => DateTime.UtcNow;
    CancellationToken CancellationToken => default;
    Guid? UserId { get; }
    bool IsPrivileged { get; }
    string CorrelationId { get; }
    string? Reason { get; }
}
