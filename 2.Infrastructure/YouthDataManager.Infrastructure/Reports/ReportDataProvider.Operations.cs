using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Exceptions;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Reports;

public partial class ReportDataProvider
{
    private readonly ICallerContext _caller;
    private readonly int _exportLimit;
    private readonly int _weekStart;
    private CancellationToken Ct => _caller.CancellationToken;
    public ReportDataProvider(AppDbContext context,ICallerContext caller,IConfiguration configuration)
    {
        _context=context;_caller=caller;
        _exportLimit=int.TryParse(configuration["Reports:ExportRowLimit"],out var limit) ? Math.Clamp(limit,1,100000) : 20000;
        _weekStart=int.TryParse(configuration["Reports:WeekStart"],out var day) && day is >=0 and <=6 ? day : 5;
    }
    private IQueryable<Student> Roster => _context.Students.AsNoTracking().Where(s=>!s.IsDeleted && (_caller.IsPrivileged || s.ServantId==_caller.UserId));
    private IQueryable<CallLog> Calls => _context.CallLogs.IgnoreQueryFilters().ForServiceYear(_context).AsNoTracking().Where(c=>!c.IsVoided && (_caller.IsPrivileged || c.ServantId==_caller.UserId));
    private IQueryable<HomeVisit> Visits => _context.HomeVisits.IgnoreQueryFilters().ForServiceYear(_context).AsNoTracking().Where(v=>!v.IsVoided && (_caller.IsPrivileged || v.ServantId==_caller.UserId || v.Participants.Any(p=>p.ServantId==_caller.UserId)));
    private IQueryable<VisitCredit> Credits => Visits.Select(v=>new VisitCredit { VisitId=v.Id,StudentId=v.StudentId,ServantId=v.ServantId,VisitDate=v.VisitDate })
        .Concat(from p in _context.HomeVisitParticipants.IgnoreQueryFilters().ForServiceYear(_context).AsNoTracking() join v in Visits on p.HomeVisitId equals v.Id select new VisitCredit {VisitId=v.Id,StudentId=v.StudentId,ServantId=p.ServantId,VisitDate=v.VisitDate}).Distinct();
    private IQueryable<ApplicationUser> People
    {
        get
        {
            var roleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);
            return _context.Users.AsNoTracking().Where(u=>(_caller.IsPrivileged || u.Id==_caller.UserId) &&
                (_context.UserRoles.Any(r=>r.UserId==u.Id && roleIds.Contains(r.RoleId)) || Calls.Any(c=>c.ServantId==u.Id) || Credits.Any(v=>v.ServantId==u.Id) || Roster.Any(s=>s.ServantId==u.Id)));
        }
    }
    private int? _configuredWeekStart;
    private async Task<(DateTime Start,DateTime End)> CurrentWeek()
    {
        _configuredWeekStart ??= await _context.Set<MeetingConfiguration>().Where(s=>s.Id==1).Select(s=>(int?)s.ReportWeekStart).SingleOrDefaultAsync(Ct) ?? _weekStart;
        var local=TimeZoneInfo.ConvertTimeFromUtc(_caller.UtcNow,BusinessTime.Zone);
        var start=local.Date.AddDays(-(((int)local.DayOfWeek-_configuredWeekStart.Value+7)%7));
        return (BusinessTime.UtcStart(start),BusinessTime.UtcStart(start.AddDays(7)));
    }
    private static void Page(int page,int size,string? key,params string[] allowed)
    {
        if (page<1 || size is <1 or >100 || (long)(page-1)*size>int.MaxValue || (key!=null && !allowed.Contains(key.ToLowerInvariant()))) throw new DomainValidationException("Invalid pagination or sort key. Page size must be 1-100.");
    }
    private (DateTime? Start,DateTime? End) Period(DateTime? from,DateTime? to)
    {
        if (from>to || from?.Year<1900 || to?.Year>9998) throw new DomainValidationException("Invalid report period.");
        return (from.HasValue ? BusinessTime.UtcStart(from.Value) : null,to.HasValue ? BusinessTime.UtcStart(to.Value.AddDays(1)) : null);
    }
    public async Task<ServantDashboardDto> ServantDashboard(Guid id)
    {
        if (!_caller.IsPrivileged && id!=_caller.UserId) throw new DomainValidationException("Invalid caller scope.");
        var (start,end)=await CurrentWeek();var q=Roster.Where(s=>s.ServantId==id);
        return new(await q.CountAsync(Ct),await Calls.CountAsync(c=>c.ServantId==id && c.CallDate>=start && c.CallDate<end,Ct),
            await Credits.CountAsync(v=>v.ServantId==id && v.VisitDate>=start && v.VisitDate<end,Ct),
            await _context.Set<FollowUpTask>().ForServiceYear(_context).Where(t=>t.AssigneeId==id && t.Status=="Open" && t.DueAt<=_caller.UtcNow).Select(t=>t.StudentId).Distinct().CountAsync(Ct),
            await q.CountAsync(s=>!Calls.Any(c=>c.StudentId==s.Id && c.ServantId==id) && !Visits.Any(v=>v.StudentId==s.Id && (v.ServantId==id || v.Participants.Any(p=>p.ServantId==id))),Ct));
    }
    public async Task<ManagerDashboardDto> ManagerDashboard()
    {
        if (!_caller.IsPrivileged) throw new DomainValidationException("Privileged report required.");
        var (start,end)=await CurrentWeek();
        var groups=await Roster.Where(s=>s.ServantId!=null).GroupBy(s=>new {s.ServantId,Name=s.Servant!.FullName,Login=s.Servant.UserName}).Select(g=>new {g.Key,Count=g.Count()}).ToListAsync(Ct);
        var assignments=groups.ToDictionary(g=>$"{g.Key.Name} ({g.Key.Login})",g=>g.Count);
        var recent=await _context.ActivityLogs.AsNoTracking().OrderByDescending(a=>a.Timestamp).ThenBy(a=>a.Id).Take(10).Select(a=>new RecentActivityDto(a.UserName+": "+a.Action+" - "+a.Details,a.Timestamp)).ToListAsync(Ct);
        var activeRoleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);
        var activeOwners=_context.Users.AsNoTracking().Where(u=>u.IsActive && _context.UserRoles.Any(r=>r.UserId==u.Id && activeRoleIds.Contains(r.RoleId)));
        return new(await Roster.CountAsync(Ct),await activeOwners.CountAsync(Ct),await Calls.CountAsync(Ct),await Visits.CountAsync(Ct),
            await Calls.CountAsync(c=>c.CallDate>=start && c.CallDate<end,Ct),await Visits.CountAsync(v=>v.VisitDate>=start && v.VisitDate<end,Ct),assignments,recent,
            await _context.Set<FollowUpTask>().ForServiceYear(_context).Where(t=>t.Status=="Open" && t.DueAt<=_caller.UtcNow).Select(t=>t.StudentId).Distinct().CountAsync(Ct),
            await Roster.CountAsync(s=>!s.CallLogs.Any(c=>!c.IsVoided) && !s.HomeVisits.Any(v=>!v.IsVoided),Ct),
            await Roster.CountAsync(s=>s.ServantId==null,Ct),
            await _context.Set<FollowUpTask>().ForServiceYear(_context).CountAsync(t=>t.Status=="Open" && t.AssigneeId==null,Ct));
    }
    public async Task<IReadOnlyList<ServantStatsDto>> Stats(IEnumerable<Guid> ids)
    {
        var list=ids.Distinct().ToArray();if(list.Length>100 || list.Any(id=>id==Guid.Empty || (!_caller.IsPrivileged && id!=_caller.UserId))) throw new DomainValidationException("Invalid servant selection.");
        return await _context.Users.AsNoTracking().Where(u=>list.Contains(u.Id)).Select(u=>new ServantStatsDto(u.Id,Roster.Count(s=>s.ServantId==u.Id),
            Calls.Where(c=>c.ServantId==u.Id).Select(c=>(DateTime?)c.CallDate).Max(),Credits.Where(v=>v.ServantId==u.Id).Select(v=>(DateTime?)v.VisitDate).Max())).ToListAsync(Ct);
    }
    public async Task<PagedReportResult<ServantPerformanceDto>> Performances(int page,int size,string? sort,bool desc,Guid? id=null)
    {
        Page(page,size,sort,"fullname","assignedstudentscount","callsthisweek","visitsthisweek");
        var (start,end)=await CurrentWeek();var people=People;
        if (id.HasValue) people=people.Where(u=>u.Id==id);
        var q=people.Select(u=>new ServantPerformanceDto { ServantId=u.Id,FullName=u.FullName,AssignedStudentsCount=Roster.Count(s=>s.ServantId==u.Id),CallsThisWeek=Calls.Count(c=>c.ServantId==u.Id && c.CallDate>=start && c.CallDate<end),VisitsThisWeek=Credits.Count(v=>v.ServantId==u.Id && v.VisitDate>=start && v.VisitDate<end) });
        var key=sort?.ToLowerInvariant();IOrderedQueryable<ServantPerformanceDto> ordered=key switch {
            "assignedstudentscount"=>desc ? q.OrderByDescending(x=>x.AssignedStudentsCount) : q.OrderBy(x=>x.AssignedStudentsCount),
            "callsthisweek"=>desc ? q.OrderByDescending(x=>x.CallsThisWeek) : q.OrderBy(x=>x.CallsThisWeek),
            "visitsthisweek"=>desc ? q.OrderByDescending(x=>x.VisitsThisWeek) : q.OrderBy(x=>x.VisitsThisWeek),
            _=>desc ? q.OrderByDescending(x=>x.FullName) : q.OrderBy(x=>x.FullName) };
        return new(await ordered.ThenBy(x=>x.ServantId).Skip((page-1)*size).Take(size).ToListAsync(Ct),await q.CountAsync(Ct),page,size);
    }
    public async Task<PagedReportResult<ServantWithStatsDto>> Servants(int page,int size,string? search,string? role,bool? active,string? sort,bool desc)
    {
        Page(page,size,sort,"fullname","username","role","isactive","assignedstudentscount","lastcalldate","lastvisitdate");
        var q=_context.Users.AsNoTracking().Where(u=>_caller.IsPrivileged || u.Id==_caller.UserId);
        if (role!=null)
        {
            if (role is not ("Admin" or "Priest" or "Manager" or "Secretary" or "Servant")) throw new DomainValidationException("Invalid role.");
            q=q.Where(u=>_context.UserRoles.Any(ur=>ur.UserId==u.Id && _context.Roles.Any(r=>r.Id==ur.RoleId && r.Name==role)));
        }
        else q=q.Where(u=>!_context.UserRoles.Any(ur=>ur.UserId==u.Id && _context.Roles.Any(r=>r.Id==ur.RoleId && (r.Name=="Admin" || r.Name=="Priest"))));
        if (active.HasValue) q=q.Where(u=>u.IsActive==active.Value);
        if (!string.IsNullOrWhiteSpace(search)) {var term=ArabicNormalizer.Normalize(search.Trim());q=q.Where(u=>u.NormalizedFullName.Contains(term) || u.UserName!.Contains(search.Trim()) || u.Phone.Contains(search.Trim()));}
        var rows=q.Select(u=>new ServantWithStatsDto {Id=u.Id,UserName=u.UserName ?? "",FullName=u.FullName,Email=u.Email ?? "",Phone=u.Phone,
            Role=(from ur in _context.UserRoles join r in _context.Roles on ur.RoleId equals r.Id where ur.UserId==u.Id orderby r.Name select r.Name).FirstOrDefault() ?? "Unknown",
            IsActive=u.IsActive,CreatedAt=u.CreatedAt,UpdatedAt=u.UpdatedAt,PhotoPath=u.PhotoPath,Address=u.Address,
            AssignedStudentsCount=Roster.Count(s=>s.ServantId==u.Id),LastCallDate=Calls.Where(c=>c.ServantId==u.Id).Select(c=>(DateTime?)c.CallDate).Max(),LastVisitDate=Credits.Where(v=>v.ServantId==u.Id).Select(v=>(DateTime?)v.VisitDate).Max() });
        IOrderedQueryable<ServantWithStatsDto> ordered=sort?.ToLowerInvariant() switch {
            "username"=>desc ? rows.OrderByDescending(x=>x.UserName) : rows.OrderBy(x=>x.UserName),
            "role"=>desc ? rows.OrderByDescending(x=>x.Role) : rows.OrderBy(x=>x.Role),
            "isactive"=>desc ? rows.OrderByDescending(x=>x.IsActive) : rows.OrderBy(x=>x.IsActive),
            "assignedstudentscount"=>desc ? rows.OrderByDescending(x=>x.AssignedStudentsCount) : rows.OrderBy(x=>x.AssignedStudentsCount),
            "lastcalldate"=>desc ? rows.OrderByDescending(x=>x.LastCallDate) : rows.OrderBy(x=>x.LastCallDate),
            "lastvisitdate"=>desc ? rows.OrderByDescending(x=>x.LastVisitDate) : rows.OrderBy(x=>x.LastVisitDate),
            _=>desc ? rows.OrderByDescending(x=>x.FullName) : rows.OrderBy(x=>x.FullName) };
        return new(await ordered.ThenBy(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync(Ct),await q.CountAsync(Ct),page,size);
    }
    public async Task<PagedReportResult<ServantActivitySummaryDto>> Activity(DateTime? from,DateTime? to,Guid? id,int page,int size,string? sort,bool desc)
    {
        Page(page,size,sort,"fullname","assignedcount","callsinperiod","visitsinperiod");
        _configuredWeekStart ??= await _context.Set<MeetingConfiguration>().Where(s=>s.Id==1).Select(s=>(int?)s.ReportWeekStart).SingleOrDefaultAsync(Ct) ?? _weekStart;
        var local=TimeZoneInfo.ConvertTimeFromUtc(_caller.UtcNow,BusinessTime.Zone);var (start,end)=Period(from ?? _caller.SelectedServiceYear?.Start ?? (_caller.AllServiceYears ? (DateTime?)null : local.Date.AddMonths(-1)),to ?? _caller.SelectedServiceYear?.LastDay ?? (_caller.AllServiceYears ? (DateTime?)null : local.Date));
        var people=People;if(id.HasValue)people=people.Where(u=>u.Id==id);
        var q=people.Select(u=>new ServantActivitySummaryDto { ServantId=u.Id,FullName=u.FullName,AssignedCount=Roster.Count(s=>s.ServantId==u.Id),CallsInPeriod=Calls.Count(c=>c.ServantId==u.Id && (!start.HasValue || c.CallDate>=start) && (!end.HasValue || c.CallDate<end)),VisitsInPeriod=Credits.Count(v=>v.ServantId==u.Id && (!start.HasValue || v.VisitDate>=start) && (!end.HasValue || v.VisitDate<end)) });
        IOrderedQueryable<ServantActivitySummaryDto> ordered=sort?.ToLowerInvariant() switch {
            "assignedcount"=>desc ? q.OrderByDescending(x=>x.AssignedCount) : q.OrderBy(x=>x.AssignedCount),
            "callsinperiod"=>desc ? q.OrderByDescending(x=>x.CallsInPeriod) : q.OrderBy(x=>x.CallsInPeriod),
            "visitsinperiod"=>desc ? q.OrderByDescending(x=>x.VisitsInPeriod) : q.OrderBy(x=>x.VisitsInPeriod),
            _=>desc ? q.OrderByDescending(x=>x.FullName) : q.OrderBy(x=>x.FullName) };
        return new(await ordered.ThenBy(x=>x.ServantId).Skip((page-1)*size).Take(size).ToListAsync(Ct),await q.CountAsync(Ct),page,size);
    }
    private IQueryable<Student> StudentFilter(ReportRequestDto f,bool history=false)
    {
        if (f.Gender is <0 or >1 || f.BirthMonth is <0 or >12 || f.ServantId==Guid.Empty) throw new DomainValidationException("Invalid report filters.");
        var q=history ? _context.Students.IgnoreQueryFilters().ForServiceYear(_context).AsNoTracking() : Roster;
        if(f.Area!=null)q=f.Area=="__missing__" ? q.Where(s=>s.Area==null || s.Area.Trim()=="") : q.Where(s=>s.Area==f.Area);
        if(f.College!=null)q=q.Where(s=>s.College==f.College);
        if(f.AcademicYear!=null)q=f.AcademicYear=="__missing__" ? q.Where(s=>s.AcademicYear==null || s.AcademicYear.Trim()=="") : q.Where(s=>s.AcademicYear==f.AcademicYear);
        if(f.Gender.HasValue)q=q.Where(s=>(int)s.Gender==f.Gender.Value);
        if(f.BirthMonth.HasValue)q=q.Where(s=>(s.BirthDate.HasValue ? s.BirthDate.Value.Month : s.BirthMonth ?? 0)==f.BirthMonth);
        if(f.HasServant.HasValue)q=f.HasServant.Value ? q.Where(s=>s.ServantId!=null) : q.Where(s=>s.ServantId==null);
        if(!string.IsNullOrWhiteSpace(f.Search)){var term=ArabicNormalizer.Normalize(f.Search.Trim());q=q.Where(s=>s.NormalizedFullName.Contains(term) || s.Phone.Contains(f.Search.Trim()));}
        return q;
    }
    private void ExportBound(int count) {if(count>_exportLimit)throw new DomainValidationException($"Export exceeds {_exportLimit:N0} rows. Narrow the filters.");}
    public async Task<IReadOnlyList<Student>> ExportStudents(ReportRequestDto f)
    {
        var q=StudentFilter(f);var (start,end)=Period(f.DateFrom,f.DateTo);
        if(f.ServantId.HasValue)q=q.Where(s=>s.ServantId==f.ServantId);
        if(start.HasValue)q=q.Where(s=>s.CreatedAt>=start);if(end.HasValue)q=q.Where(s=>s.CreatedAt<end);
        var rows=await q.Include(s=>s.Servant).OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Take(_exportLimit+1).ToListAsync(Ct);ExportBound(rows.Count);return rows;
    }
    public async Task<IReadOnlyList<CallLog>> ExportCalls(ReportRequestDto f)
    {
        var ids=StudentFilter(f,true).Select(s=>s.Id);var q=Calls.Where(c=>ids.Contains(c.StudentId));var (start,end)=Period(f.DateFrom,f.DateTo);
        if(f.ServantId.HasValue)q=q.Where(c=>c.ServantId==f.ServantId);if(start.HasValue)q=q.Where(c=>c.CallDate>=start);if(end.HasValue)q=q.Where(c=>c.CallDate<end);
        var rows=await q.Include(c=>c.Student).Include(c=>c.Servant).OrderBy(c=>c.CallDate).ThenBy(c=>c.Id).Take(_exportLimit+1).ToListAsync(Ct);ExportBound(rows.Count);return rows;
    }
    public async Task<IReadOnlyList<HomeVisit>> ExportVisits(ReportRequestDto f)
    {
        var ids=StudentFilter(f,true).Select(s=>s.Id);var q=Visits.Where(v=>ids.Contains(v.StudentId));var (start,end)=Period(f.DateFrom,f.DateTo);
        if(f.ServantId.HasValue)q=q.Where(v=>v.ServantId==f.ServantId || v.Participants.Any(p=>p.ServantId==f.ServantId));if(start.HasValue)q=q.Where(v=>v.VisitDate>=start);if(end.HasValue)q=q.Where(v=>v.VisitDate<end);
        var rows=await q.Include(v=>v.Student).Include(v=>v.Servant).Include(v=>v.Participants).ThenInclude(p=>p.Servant).AsSplitQuery().OrderBy(v=>v.VisitDate).ThenBy(v=>v.Id).Take(_exportLimit+1).ToListAsync(Ct);ExportBound(rows.Count);return rows;
    }
    private sealed class VisitCredit { public Guid VisitId { get; init; } public Guid StudentId { get; init; } public Guid ServantId { get; init; } public DateTime VisitDate { get; init; } }
}
