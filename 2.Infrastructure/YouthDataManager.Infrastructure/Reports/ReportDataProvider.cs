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

    public async Task<(IReadOnlyList<StudentNoContactDto> Items, int TotalCount)> GetStudentsWithNoRecentContactPagedAsync(int days, Guid? servantId, int page, int pageSize, string? sortBy = null, bool sortDesc = false)
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
        var sortKey = (sortBy ?? "fullname").Trim().ToLowerInvariant();

        var items = sortKey == "servantname"
            ? await (sortDesc ? baseQuery.OrderByDescending(x => x.ServantName).ThenBy(x => x.FullName) : baseQuery.OrderBy(x => x.ServantName).ThenBy(x => x.FullName))
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new StudentNoContactDto(x.Id, x.FullName ?? "", x.ServantId, x.ServantName, x.LastContact)).ToListAsync()
            : sortKey == "lastcontactdate"
            ? await (sortDesc ? baseQuery.OrderByDescending(x => x.LastContact).ThenBy(x => x.FullName) : baseQuery.OrderBy(x => x.LastContact).ThenBy(x => x.FullName))
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new StudentNoContactDto(x.Id, x.FullName ?? "", x.ServantId, x.ServantName, x.LastContact)).ToListAsync()
            : await (sortDesc ? baseQuery.OrderByDescending(x => x.FullName) : baseQuery.OrderBy(x => x.FullName))
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new StudentNoContactDto(x.Id, x.FullName ?? "", x.ServantId, x.ServantName, x.LastContact)).ToListAsync();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAreaPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false)
    {
        var grouped = _context.Students
            .AsNoTracking()
            .GroupBy(s => s.Area ?? "")
            .Select(g => new { g.Key, Count = g.Count() });

        var totalCount = await grouped.CountAsync();
        var sortKey = (sortBy ?? "groupkey").Trim().ToLowerInvariant();
        var ordered = sortKey == "count"
            ? (sortDesc ? grouped.OrderByDescending(x => x.Count).ThenBy(x => x.Key) : grouped.OrderBy(x => x.Count).ThenBy(x => x.Key))
            : (sortDesc ? grouped.OrderByDescending(x => x.Key) : grouped.OrderBy(x => x.Key));
        var pageOfGroups = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = pageOfGroups.Select(x => new StudentsByGroupDto(x.Key, x.Count, null)).ToList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByAcademicYearPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false)
    {
        var grouped = _context.Students
            .AsNoTracking()
            .GroupBy(s => s.AcademicYear ?? "")
            .Select(g => new { g.Key, Count = g.Count() });

        var totalCount = await grouped.CountAsync();
        var sortKey = (sortBy ?? "groupkey").Trim().ToLowerInvariant();
        var ordered = sortKey == "count"
            ? (sortDesc ? grouped.OrderByDescending(x => x.Count).ThenBy(x => x.Key) : grouped.OrderBy(x => x.Count).ThenBy(x => x.Key))
            : (sortDesc ? grouped.OrderByDescending(x => x.Key) : grouped.OrderBy(x => x.Key));
        var pageOfGroups = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = pageOfGroups.Select(x => new StudentsByGroupDto(x.Key, x.Count, null)).ToList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<StudentsByGroupDto> Items, int TotalCount)> GetStudentsByBirthMonthPagedAsync(int page, int pageSize, string? sortBy = null, bool sortDesc = false)
    {
        var grouped = _context.Students
            .AsNoTracking()
            .GroupBy(s => s.BirthMonth ?? (s.BirthDate.HasValue ? s.BirthDate.Value.Month : 0))
            .Select(g => new { Key = g.Key.ToString(), Count = g.Count(), SortKey = g.Key == 0 ? 13 : g.Key });

        var totalCount = await grouped.CountAsync();
        var sortKey = (sortBy ?? "groupkey").Trim().ToLowerInvariant();
        var ordered = sortKey == "count"
            ? (sortDesc ? grouped.OrderByDescending(x => x.Count).ThenBy(x => x.SortKey) : grouped.OrderBy(x => x.Count).ThenBy(x => x.SortKey))
            : (sortDesc ? grouped.OrderByDescending(x => x.SortKey) : grouped.OrderBy(x => x.SortKey));
        var pageOfGroups = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var items = pageOfGroups.Select(x => new StudentsByGroupDto(x.Key, x.Count, null)).ToList();
        return (items, totalCount);
    }
}
