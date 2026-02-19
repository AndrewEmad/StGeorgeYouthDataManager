using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Reports.Service.Abstractions.Data;
using YouthDataManager.Reports.Service.Abstractions.DTOs;

namespace YouthDataManager.Infrastructure.Reports;

public class ReportDataProvider : IReportDataProvider
{
    private readonly AppDbContext _context;

    public ReportDataProvider(AppDbContext context) => _context = context;

    public async Task<(IReadOnlyList<StudentNoContactDto> Items, int TotalCount)> GetStudentsWithNoRecentContactPagedAsync(int days, Guid? servantId, int page, int pageSize)
    {
        var cutoff = DateTime.UtcNow.Date.AddDays(-days);
        var baseQuery = _context.Students
            .AsNoTracking()
            .Where(s => !servantId.HasValue || s.ServantId == servantId)
            .Select(s => new
            {
                s.Id,
                s.FullName,
                s.ServantId,
                ServantName = s.Servant != null ? s.Servant.FullName : null,
                LastContact = s.CallLogs.Select(c => (DateTime?)c.CallDate).Concat(s.HomeVisits.Select(v => (DateTime?)v.VisitDate)).Max()
            })
            .Where(x => x.LastContact == null || x.LastContact < cutoff);

        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StudentNoContactDto(x.Id, x.FullName ?? "", x.ServantId, x.ServantName, x.LastContact))
            .ToListAsync();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAreaPagedAsync(int page, int pageSize)
    {
        var baseQuery = _context.Students
            .AsNoTracking()
            .GroupBy(s => s.Area ?? "")
            .OrderBy(g => g.Key)
            .Select(g => new { g.Key, Count = g.Count() });

        var totalCount = await baseQuery.CountAsync();
        var pageOfGroups = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = pageOfGroups.Select(x => new StudentsByGroupDto(x.Key, x.Count, null)).ToList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAcademicYearPagedAsync(int page, int pageSize)
    {
        var baseQuery = _context.Students
            .AsNoTracking()
            .GroupBy(s => s.AcademicYear ?? "")
            .OrderBy(g => g.Key)
            .Select(g => new { g.Key, Count = g.Count() });

        var totalCount = await baseQuery.CountAsync();
        var pageOfGroups = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = pageOfGroups.Select(x => new StudentsByGroupDto(x.Key, x.Count, null)).ToList();
        return (items, totalCount);
    }
}
