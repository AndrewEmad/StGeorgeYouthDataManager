using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController, Route("api/activity-history"), Authorize]
public sealed class ActivityHistoryController(IApplicationStore store, ResourceAccess access) : ControllerBase
{
    private async Task<bool> Allowed(Guid? studentId, Guid? actorId, CancellationToken ct)
    {
        if (actorId.HasValue && !access.Privileged && actorId != access.UserId) return false;
        return !studentId.HasValue || await store.Query<Student>(false, access.Privileged)
            .AnyAsync(s => s.Id == studentId && (access.Privileged || s.ServantId == access.UserId), ct);
    }
    private static bool Valid(int page, int pageSize) => page >= 1 && pageSize is >= 1 and <= 100 && (long)(page-1)*pageSize <= int.MaxValue;

    [HttpGet("calls")]
    public async Task<IActionResult> Calls(Guid? studentId = null, Guid? actorId = null, int page = 1, int pageSize = 25, Guid? recordId=null,CancellationToken ct = default)
    {
        if (!Valid(page,pageSize) || studentId==Guid.Empty || actorId==Guid.Empty || recordId==Guid.Empty) return BadRequest("Invalid pagination.");
        if (!await Allowed(studentId,actorId,ct)) return Forbid();
        var q=store.Query<CallLog>(false,true);if(recordId.HasValue)q=q.Where(c=>c.Id==recordId);
        if (studentId.HasValue) q=q.Where(c=>c.StudentId==studentId);
        if (actorId.HasValue) q=q.Where(c=>c.ServantId==actorId);
        else if (!studentId.HasValue && !access.Privileged) q=q.Where(c=>c.ServantId==access.UserId);
        var rows=await q.OrderByDescending(c=>c.CallDate).ThenByDescending(c=>c.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(c=>new {c.Id,c.StudentId,StudentName=c.Student.FullName,c.CallDate,c.CallStatus,c.Notes,c.NextFollowUpDate,
                RecordedByServantId=c.ServantId,RecordedByName=c.Servant.FullName,c.IsVoided,c.VoidReason,c.Version,
                CanCorrect=access.Privileged || (c.ServantId==access.UserId && !c.Student.IsDeleted && c.Student.ServantId==access.UserId)})
            .ToListAsync(ct);
        return Ok(new {Items=rows,TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }

    [HttpGet("visits")]
    public async Task<IActionResult> Visits(Guid? studentId = null, Guid? actorId = null, int page = 1, int pageSize = 25, Guid? recordId=null,CancellationToken ct = default)
    {
        if (!Valid(page,pageSize) || studentId==Guid.Empty || actorId==Guid.Empty || recordId==Guid.Empty) return BadRequest("Invalid pagination.");
        if (!await Allowed(studentId,actorId,ct)) return Forbid();
        var q=store.Query<HomeVisit>(false,true);if(recordId.HasValue)q=q.Where(v=>v.Id==recordId);
        if (studentId.HasValue) q=q.Where(v=>v.StudentId==studentId);
        if (actorId.HasValue) q=q.Where(v=>v.ServantId==actorId || v.Participants.Any(p=>p.ServantId==actorId));
        else if (!studentId.HasValue && !access.Privileged) q=q.Where(v=>v.ServantId==access.UserId || v.Participants.Any(p=>p.ServantId==access.UserId));
        var rows=await q.OrderByDescending(v=>v.VisitDate).ThenByDescending(v=>v.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(v=>new {v.Id,v.StudentId,StudentName=v.Student.FullName,v.VisitDate,v.VisitOutcome,v.Notes,v.NextVisitDate,
                RecordedByServantId=v.ServantId,RecordedByName=v.Servant.FullName,v.IsVoided,v.VoidReason,v.Version,
                Participants=v.Participants.Select(p=>new {p.ServantId,ServantName=p.Servant.FullName}).ToList(),
                CanCorrect=access.Privileged || (v.ServantId==access.UserId && !v.Student.IsDeleted && v.Student.ServantId==access.UserId)})
            .ToListAsync(ct);
        return Ok(new {Items=rows,TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }
}
