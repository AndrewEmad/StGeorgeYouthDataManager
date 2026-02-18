using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.NoTracking
{
    public interface IActivityLogNoTrackingRepository
    {
        Task<IEnumerable<T>> GetRecent<T>(int count, Expression<Func<ActivityLog, T>> selector);
        Task<IEnumerable<T>> GetAll<T>(Expression<Func<ActivityLog, T>> selector);
    }
}
