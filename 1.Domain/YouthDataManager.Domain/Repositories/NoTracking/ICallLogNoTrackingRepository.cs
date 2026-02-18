using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YouthDataManager.Domain.Repositories.NoTracking;

public interface ICallLogNoTrackingRepository
{
    Task<IEnumerable<T>> GetByStudentId<T>(Guid studentId, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.CallLog, T>> selector);
    Task<IEnumerable<T>> GetByServantId<T>(Guid servantId, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.CallLog, T>> selector);
    Task<IEnumerable<T>> GetByFilter<T>(DateTime? from, DateTime? to, Guid? servantId, string? area, string? college, System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.CallLog, T>> selector);
    Task<IEnumerable<T>> GetAll<T>(System.Linq.Expressions.Expression<Func<YouthDataManager.Domain.Entities.CallLog, T>> selector);
}
