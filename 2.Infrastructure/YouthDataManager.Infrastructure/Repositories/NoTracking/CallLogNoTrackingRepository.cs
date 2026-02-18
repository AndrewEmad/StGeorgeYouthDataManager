using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.NoTracking;

public class CallLogNoTrackingRepository : ICallLogNoTrackingRepository
{
    private readonly AppDbContext _context;

    public CallLogNoTrackingRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<T>> GetByStudentId<T>(Guid studentId, Expression<Func<CallLog, T>> selector)
    {
        return await _context.CallLogs
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.CallDate)
            .Select(selector)
            .ToListAsync();
    }

    public async Task<IEnumerable<T>> GetByServantId<T>(Guid servantId, Expression<Func<CallLog, T>> selector)
    {
        return await _context.CallLogs
            .AsNoTracking()
            .Where(e => e.ServantId == servantId)
            .OrderByDescending(e => e.CallDate)
            .Select(selector)
            .ToListAsync();
    }

    public async Task<IEnumerable<T>> GetByFilter<T>(
        DateTime? from, 
        DateTime? to, 
        Guid? servantId, 
        string? area, 
        string? college, 
        Expression<Func<CallLog, T>> selector)
    {
        var query = _context.CallLogs.AsNoTracking();

        if (from.HasValue) query = query.Where(e => e.CallDate >= from.Value);
        if (to.HasValue) query = query.Where(e => e.CallDate <= to.Value);
        if (servantId.HasValue) query = query.Where(e => e.ServantId == servantId.Value);
        if (!string.IsNullOrEmpty(area)) query = query.Where(e => e.Student.Area == area);
        if (!string.IsNullOrEmpty(college)) query = query.Where(e => e.Student.College == college);

        return await query.OrderByDescending(e => e.CallDate).Select(selector).ToListAsync();
    }

    public async Task<IEnumerable<T>> GetAll<T>(Expression<Func<CallLog, T>> selector)
    {
        return await _context.CallLogs
            .AsNoTracking()
            .OrderByDescending(e => e.CallDate)
            .Select(selector)
            .ToListAsync();
    }
}
