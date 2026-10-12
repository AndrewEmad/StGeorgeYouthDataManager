using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Utilities;
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

    private static bool ValidFilters(DateTime? from,DateTime? to,string? search,string? status) =>
        (!from.HasValue || !to.HasValue || from<=to) && !(from?.Year<1900 || from?.Year>9998 || to?.Year<1900 || to?.Year>9998) &&
        (!from.HasValue || from.Value.TimeOfDay==TimeSpan.Zero) && (!to.HasValue || to.Value.TimeOfDay==TimeSpan.Zero) &&
        (search?.Trim().Length ?? 0)<=200 && (status==null || status is "Active" or "Voided");

    // Unqualified reads are personal for every role; explicit student/actor reads retain resource authorization.
    [HttpGet("calls")]
    public async Task<IActionResult> Calls(Guid? studentId = null, Guid? actorId = null, int page = 1, int pageSize = 25, Guid? recordId=null,DateTime? dateFrom=null,DateTime? dateTo=null,string? search=null,int? outcome=null,string? status=null,CancellationToken ct = default)
    {
        if (!Valid(page,pageSize) || studentId==Guid.Empty || actorId==Guid.Empty || recordId==Guid.Empty) return BadRequest("Invalid pagination.");
        if (!ValidFilters(dateFrom,dateTo,search,status) || (outcome.HasValue && !Enum.IsDefined(typeof(CallStatus),outcome.Value))) return BadRequest("فلاتر السجل غير صحيحة.");
        if (!await Allowed(studentId,actorId,ct)) return Forbid();
        var q=store.Query<CallLog>(false,true);if(recordId.HasValue)q=q.Where(c=>c.Id==recordId);
        if (studentId.HasValue) q=q.Where(c=>c.StudentId==studentId);
        if (actorId.HasValue) q=q.Where(c=>c.ServantId==actorId);
        else if (!studentId.HasValue) q=q.Where(c=>c.ServantId==access.UserId);
        if(dateFrom.HasValue){var start=BusinessTime.UtcStart(dateFrom.Value);q=q.Where(c=>c.CallDate>=start);}
        if(dateTo.HasValue){var end=BusinessTime.UtcStart(dateTo.Value.AddDays(1));q=q.Where(c=>c.CallDate<end);}
        if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();var normalized=ArabicNormalizer.Normalize(term);q=q.Where(c=>c.Student.NormalizedFullName.Contains(normalized) || c.Student.FullName.Contains(term) || c.Notes.Contains(term));}
        if(outcome.HasValue)q=q.Where(c=>c.CallStatus==(CallStatus)outcome.Value);
        if(status!=null)q=q.Where(c=>c.IsVoided==(status=="Voided"));
        var rows=await q.OrderByDescending(c=>c.CallDate).ThenByDescending(c=>c.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(c=>new {c.Id,c.StudentId,StudentName=c.Student.FullName,c.CallDate,c.CallStatus,c.Notes,c.NextFollowUpDate,
                RecordedByServantId=c.ServantId,RecordedByName=c.Servant.FullName,c.IsVoided,c.VoidReason,c.Version,
                CanCorrect=access.Privileged || (c.ServantId==access.UserId && !c.Student.IsDeleted && c.Student.ServantId==access.UserId)})
            .ToListAsync(ct);
        return Ok(new {Items=rows,TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }

    [HttpGet("visits")]
    public async Task<IActionResult> Visits(Guid? studentId = null, Guid? actorId = null, int page = 1, int pageSize = 25, Guid? recordId=null,DateTime? dateFrom=null,DateTime? dateTo=null,string? search=null,int? outcome=null,string? status=null,CancellationToken ct = default)
    {
        if (!Valid(page,pageSize) || studentId==Guid.Empty || actorId==Guid.Empty || recordId==Guid.Empty) return BadRequest("Invalid pagination.");
        if (!ValidFilters(dateFrom,dateTo,search,status) || (outcome.HasValue && !Enum.IsDefined(typeof(VisitOutcome),outcome.Value))) return BadRequest("فلاتر السجل غير صحيحة.");
        if (!await Allowed(studentId,actorId,ct)) return Forbid();
        var q=store.Query<HomeVisit>(false,true);if(recordId.HasValue)q=q.Where(v=>v.Id==recordId);
        if (studentId.HasValue) q=q.Where(v=>v.StudentId==studentId);
        if (actorId.HasValue) q=q.Where(v=>v.ServantId==actorId || v.Participants.Any(p=>p.ServantId==actorId));
        else if (!studentId.HasValue) q=q.Where(v=>v.ServantId==access.UserId || v.Participants.Any(p=>p.ServantId==access.UserId));
        if(dateFrom.HasValue){var start=BusinessTime.UtcStart(dateFrom.Value);q=q.Where(v=>v.VisitDate>=start);}
        if(dateTo.HasValue){var end=BusinessTime.UtcStart(dateTo.Value.AddDays(1));q=q.Where(v=>v.VisitDate<end);}
        if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();var normalized=ArabicNormalizer.Normalize(term);q=q.Where(v=>v.Student.NormalizedFullName.Contains(normalized) || v.Student.FullName.Contains(term) || v.Notes.Contains(term));}
        if(outcome.HasValue)q=q.Where(v=>v.VisitOutcome==(VisitOutcome)outcome.Value);
        if(status!=null)q=q.Where(v=>v.IsVoided==(status=="Voided"));
        var rows=await q.OrderByDescending(v=>v.VisitDate).ThenByDescending(v=>v.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(v=>new {v.Id,v.StudentId,StudentName=v.Student.FullName,v.VisitDate,v.VisitOutcome,v.Notes,v.NextVisitDate,
                RecordedByServantId=v.ServantId,RecordedByName=v.Servant.FullName,v.IsVoided,v.VoidReason,v.Version,
                Participants=v.Participants.Select(p=>new {p.ServantId,ServantName=p.Servant.FullName}).ToList(),
                CanCorrect=access.Privileged || (v.ServantId==access.UserId && !v.Student.IsDeleted && v.Student.ServantId==access.UserId)})
            .ToListAsync(ct);
        return Ok(new {Items=rows,TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }
}
