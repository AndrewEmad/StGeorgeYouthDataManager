using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.NoTracking;

public interface IStudentEditLogNoTrackingRepository
{
    Task<IEnumerable<T>> GetByStudentId<T>(Guid studentId, System.Linq.Expressions.Expression<Func<StudentEditLog, T>> selector);
}
