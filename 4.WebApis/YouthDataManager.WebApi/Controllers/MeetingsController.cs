using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController, Route("api/meetings"), Authorize(Roles=RolePolicies.CanRecordStudentAttendance)]
public sealed class MeetingsController(IMeetingService service,IApplicationStore store,ResourceAccess access,IConfiguration configuration) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<IActionResult> Settings(CancellationToken ct) => Ok(await store.Query<MeetingConfiguration>(false).SingleOrDefaultAsync(s=>s.Id==1,ct) ?? new MeetingConfiguration {MeetingDay=configuration.GetValue("Attendance:MeetingDay",5),ReportWeekStart=configuration.GetValue("Reports:WeekStart",5)});
    [HttpPut("settings"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Settings([FromBody] UpdateMeetingSettings dto,CancellationToken ct)
    {
        if(dto.MeetingDay is <0 or >6 || dto.ReportWeekStart is <0 or >6 || string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length>2000)return BadRequest("Valid days and a reason are required.");
        var value=await store.Query<MeetingConfiguration>().SingleOrDefaultAsync(s=>s.Id==1,ct);
        if(dto.Version!=null && dto.Version!=Convert.ToBase64String(value?.Version ?? []))return Conflict("Meeting settings changed.");
        if(value==null){value=new();store.Add(value);}
        value.MeetingDay=dto.MeetingDay;value.ReportWeekStart=dto.ReportWeekStart;await store.Save(ct);return Ok(value);
    }
    [HttpGet("recurrence")]
    public async Task<IActionResult> Recurrence(CancellationToken ct)
    {
        var settings=await store.Query<MeetingConfiguration>(false).SingleOrDefaultAsync(s=>s.Id==1,ct);
        var day=settings?.MeetingDay ?? configuration.GetValue("Attendance:MeetingDay",5);var today=BusinessTime.LocalNow.Date;
        var first=today.AddDays((day-(int)today.DayOfWeek+7)%7);var dates=Enumerable.Range(0,12).Select(n=>first.AddDays(n*7)).ToArray();
        var existing=await store.Query<MeetingSession>(false).Where(m=>dates.Contains(m.Date)).ToListAsync(ct);
        return Ok(dates.Select(d=>new {Date=d,Session=existing.SingleOrDefault(m=>m.Date==d)}));
    }
    [HttpGet("{id:guid}/eligibility"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Eligibility(Guid id,CancellationToken ct) => Ok(await store.Query<MeetingRosterEntry>(false).Where(r=>r.SessionId==id).Select(r=>new {r.PersonId,r.Kind,r.Present}).ToListAsync(ct));
    [HttpGet("{id:guid}/eligibility-candidates"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Candidates(Guid id,string kind="Student",string? search=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if(kind is not ("Student" or "Servant") || page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue)return BadRequest("Invalid query.");
        var q=kind=="Student" ? store.Query<Student>(false,true).Select(s=>new {s.Id,Name=s.FullName}) : store.Query<ApplicationUser>(false).Select(u=>new {u.Id,Name=u.FullName});
        if(!string.IsNullOrWhiteSpace(search))q=q.Where(p=>p.Name.Contains(search.Trim()));
        return Ok(new {Items=await q.OrderBy(p=>p.Name).ThenBy(p=>p.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct),TotalCount=await q.CountAsync(ct)});
    }
    [HttpPut("{id:guid}/eligibility"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Eligibility(Guid id,[FromBody] ReviewEligibility dto,CancellationToken ct)
    {
        var session=await store.Query<MeetingSession>(false).SingleOrDefaultAsync(s=>s.Id==id,ct);if(session==null)return NotFound();
        if(dto.Version!=Convert.ToBase64String(session.Version))return Conflict("Meeting changed.");
        return Result(await service.ReviewEligibility(id,dto.StudentIds,dto.ServantIds,dto.Reason));
    }
    [HttpGet]
    public async Task<IActionResult> List(DateTime? date=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if (page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue) return BadRequest("Invalid pagination.");
        var q=store.Query<MeetingSession>(false);if(date.HasValue)q=q.Where(s=>s.Date==date.Value.Date);
        return Ok(new { Items=await q.OrderByDescending(s=>s.Date).ThenBy(s=>s.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct),TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize });
    }
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartMeeting dto)
    {
        var r=await service.Start(dto.Date);
        return r.Status==ServiceResultStatus.Success ? Ok(r.Data) : BadRequest(r);
    }
    [HttpGet("{id:guid}/roster")]
    public async Task<IActionResult> Roster(Guid id,string kind="Student",string? search=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if (kind is not ("Student" or "Servant") || page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue) return BadRequest("Invalid roster query.");
        if (kind=="Servant" && !access.Privileged) return Forbid();
        var q=store.Query<MeetingRosterEntry>(false).Where(r=>r.SessionId==id && r.Kind==kind);
        if (!string.IsNullOrWhiteSpace(search)) q=q.Where(r=>r.Name.Contains(search.Trim()));
        return Ok(new { Items=await q.OrderBy(r=>r.Name).ThenBy(r=>r.PersonId).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct),TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize });
    }
    [HttpPut("{id:guid}/marks")]
    public async Task<IActionResult> Mark(Guid id,[FromBody] MeetingMark dto)
    {
        if (dto.Kind=="Servant" && !access.Privileged) return Forbid();
        var session=await store.Query<MeetingSession>(false).SingleOrDefaultAsync(s=>s.Id==id);
        if(session==null)return NotFound();
        if(dto.Version!=null && dto.Version!=Convert.ToBase64String(session.Version))return Conflict("Meeting changed. Refresh before retrying.");
        var r=await service.Mark(id,dto.Kind,dto.Ids,dto.Present,dto.Reason);
        return Result(r);
    }
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id,[FromBody] MeetingStatus dto)
    {
        var session=await store.Query<MeetingSession>(false).SingleOrDefaultAsync(s=>s.Id==id);
        if(session==null)return NotFound();
        if(session.Status==dto.Status)return Ok();
        if(dto.Version!=null && dto.Version!=Convert.ToBase64String(session.Version))return Conflict("Meeting changed. Refresh before retrying.");
        return Result(await service.ChangeStatus(id,dto.Status,dto.Reason));
    }
    private IActionResult Result(ServiceResult r) => r.Status==ServiceResultStatus.Success ? Ok() : r.Errors.Any(e=>e.Code=="Conflict") ? Conflict(r) : BadRequest(r);
}
public record StartMeeting(DateTime Date);
public record MeetingMark(string Kind,List<Guid> Ids,bool Present,string? Reason,string? Version=null);
public record MeetingStatus(string Status,string? Reason,string? Version=null);
public record UpdateMeetingSettings(int MeetingDay,int ReportWeekStart,string Reason,string? Version);
public record ReviewEligibility(List<Guid> StudentIds,List<Guid> ServantIds,string Reason,string Version);
