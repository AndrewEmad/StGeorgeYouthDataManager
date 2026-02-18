namespace YouthDataManager.Domain.Repositories.Tracking;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
