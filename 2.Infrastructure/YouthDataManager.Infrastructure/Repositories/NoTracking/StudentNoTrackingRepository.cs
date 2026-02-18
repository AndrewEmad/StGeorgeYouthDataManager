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

public class StudentNoTrackingRepository : IStudentNoTrackingRepository
{
    private readonly AppDbContext _context;

    public StudentNoTrackingRepository(AppDbContext context) => _context = context;

    public async Task<T?> GetById<T>(Guid id, Expression<Func<Student, T>> selector)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<T>> GetAll<T>(Expression<Func<Student, T>> selector)
    {
        return await _context.Students
            .AsNoTracking()
            .Select(selector)
            .ToListAsync();
    }

    public async Task<IEnumerable<T>> GetByServantId<T>(Guid servantId, Expression<Func<Student, T>> selector)
    {
        return await _context.Students
            .AsNoTracking()
            .Where(e => e.ServantId == servantId)
            .OrderBy(s => s.FullName)
            .Select(selector)
            .ToListAsync();
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPaged<T>(string? search, string? area, string? academicYear, int? gender, Guid? servantId, bool? hasServant, string? sortBy, bool sortDesc, int page, int pageSize, Expression<Func<Student, T>> selector)
    {
        var query = _context.Students.AsNoTracking().AsQueryable();
        var searchTrim = search?.Trim();
        if (!string.IsNullOrEmpty(searchTrim))
        {
            var lower = searchTrim.ToLower();
            query = query.Where(s =>
                (s.FullName != null && s.FullName.ToLower().Contains(lower)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(lower)) ||
                (s.Area != null && s.Area.ToLower().Contains(lower)) ||
                (s.College != null && s.College.ToLower().Contains(lower)));
        }
        if (!string.IsNullOrEmpty(area)) query = query.Where(s => s.Area == area);
        if (!string.IsNullOrEmpty(academicYear)) query = query.Where(s => s.AcademicYear == academicYear);
        if (gender.HasValue) query = query.Where(s => (int)s.Gender == gender.Value);
        if (servantId.HasValue) query = query.Where(s => s.ServantId == servantId.Value);
        if (hasServant == true) query = query.Where(s => s.ServantId != null);
        if (hasServant == false) query = query.Where(s => s.ServantId == null);

        var totalCount = await query.CountAsync();
        var sortKey = (sortBy ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(sortKey)) sortKey = "fullname";

        IOrderedQueryable<Student> ordered;
        switch (sortKey)
        {
            case "academicyear":
                ordered = sortDesc ? query.OrderByDescending(s => s.AcademicYear).ThenBy(s => s.Id) : query.OrderBy(s => s.AcademicYear).ThenBy(s => s.Id);
                break;
            case "servantname":
                ordered = sortDesc ? query.OrderByDescending(s => s.Servant != null ? s.Servant.FullName : null).ThenBy(s => s.Id) : query.OrderBy(s => s.Servant != null ? s.Servant.FullName : null).ThenBy(s => s.Id);
                break;
            case "lastfollowupdate":
                ordered = sortDesc ? query.OrderByDescending(s => s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max()).ThenBy(s => s.Id) : query.OrderBy(s => s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max()).ThenBy(s => s.Id);
                break;
            default:
                ordered = sortDesc ? query.OrderByDescending(s => s.FullName).ThenBy(s => s.Id) : query.OrderBy(s => s.FullName).ThenBy(s => s.Id);
                break;
        }

        var pagedQuery = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSingleQuery();
        var items = await pagedQuery.Select(selector).ToListAsync();
        return (items, totalCount);
    }

    public async Task<IEnumerable<string>> GetDistinctAreasAsync()
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.Area != null && s.Area != "")
            .Select(s => s.Area!)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetDistinctAcademicYearsAsync()
    {
        return await _context.Students
            .AsNoTracking()
            .Where(s => s.AcademicYear != null && s.AcademicYear != "")
            .Select(s => s.AcademicYear!)
            .Distinct()
            .OrderBy(y => y)
            .ToListAsync();
    }
}
