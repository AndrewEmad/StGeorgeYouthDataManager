using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.NoTracking
{
    public class ActivityLogNoTrackingRepository : IActivityLogNoTrackingRepository
    {
        private readonly AppDbContext _context;

        public ActivityLogNoTrackingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> GetRecent<T>(int count, Expression<Func<ActivityLog, T>> selector)
        {
            return await _context.ActivityLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .Select(selector)
                .ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAll<T>(Expression<Func<ActivityLog, T>> selector)
        {
            return await _context.ActivityLogs
                .AsNoTracking()
                .OrderByDescending(a => a.Timestamp)
                .Select(selector)
                .ToListAsync();
        }
    }
}
