using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Implementations.Commands;

public sealed class FollowUpCommandsService(IApplicationStore store,ICallerContext caller) : IFollowUpCommandsService
{
    private async Task<string?> Validate(Guid studentId,DateTime date,int outcome,Type outcomeType,string? notes,DateTime? next,IReadOnlyList<Guid>? participants=null,bool historical=false)
    {
        if (date.Year<2000 || date.ToUniversalTime()>caller.UtcNow.AddMinutes(5) || !Enum.IsDefined(outcomeType,outcome) || (notes?.Length ?? 0)>2000 || (next.HasValue && (next.Value.Year<2000 || next<date))) return "Invalid activity date, outcome, notes or scheduled date.";
        if (!await store.Query<Student>(false,historical && caller.IsPrivileged).AnyAsync(s=>s.Id==studentId && (caller.IsPrivileged || s.ServantId==caller.UserId), caller.CancellationToken)) return "Forbidden";
        if (participants!=null)
        {
            var ids=participants.Distinct().Where(id=>id!=caller.UserId).ToArray();
            if (ids.Length>100 || await EligibleUsers.Query(store).CountAsync(u=>ids.Contains(u.Id), caller.CancellationToken)!=ids.Length) return "Participants must be active and eligible.";
        }
        return null;
    }
    private static ServiceResult<Guid> Fail(string error) => ServiceResult<Guid>.Failure(error,new ServiceError(error=="Forbidden" ? "Forbidden" : "Validation"));
    private async Task Schedule(Guid studentId,DateTime? due,string type,Guid source)
    {
        if (!due.HasValue || !await store.Query<Student>(false).AnyAsync(s=>s.Id==studentId, caller.CancellationToken)) return;
        var owner=await store.Query<Student>(false).Where(s=>s.Id==studentId).Select(s=>s.ServantId).SingleAsync(caller.CancellationToken);
        if (owner.HasValue && !await EligibleUsers.Query(store).AnyAsync(u=>u.Id==owner, caller.CancellationToken)) owner=null;
        store.Add(new FollowUpTask { StudentId=studentId,AssigneeId=owner,Type=type,DueAt=due.Value.ToUniversalTime(),SourceId=source,SourceType=type=="Call" ? "CallLog" : "HomeVisit" });
    }
    public Task<ServiceResult<Guid>> RegisterCall(CreateCallLogRequest r,Guid currentUserId) => store.Transaction(async () =>
    {
        var error=await Validate(r.StudentId,r.CallDate,(int)r.CallStatus,typeof(YouthDataManager.Domain.Enums.CallStatus),r.Notes,r.NextFollowUpDate);
        if (error!=null) return Fail(error);
        var call=new CallLog { Id=Guid.NewGuid(),StudentId=r.StudentId,ServantId=currentUserId,CallDate=r.CallDate.ToUniversalTime(),CallStatus=r.CallStatus,Notes=r.Notes?.Trim() ?? "",NextFollowUpDate=r.NextFollowUpDate?.ToUniversalTime() };
        store.Add(call); await Schedule(r.StudentId,r.NextFollowUpDate,"Call",call.Id);
        return ServiceResult<Guid>.Success(call.Id);
    },r=>r.Status==ServiceResultStatus.Success);
    public Task<ServiceResult<Guid>> RegisterVisit(CreateHomeVisitRequest r,Guid currentUserId) => store.Transaction(async () =>
    {
        var error=await Validate(r.StudentId,r.VisitDate,(int)r.VisitOutcome,typeof(YouthDataManager.Domain.Enums.VisitOutcome),r.Notes,r.NextVisitDate,r.ParticipantServantIds);
        if (error!=null) return Fail(error);
        var visit=new HomeVisit { Id=Guid.NewGuid(),StudentId=r.StudentId,ServantId=currentUserId,VisitDate=r.VisitDate.ToUniversalTime(),VisitOutcome=r.VisitOutcome,Notes=r.Notes?.Trim() ?? "",NextVisitDate=r.NextVisitDate?.ToUniversalTime() };
        foreach (var id in (r.ParticipantServantIds ?? []).Append(currentUserId).Distinct()) visit.Participants.Add(new HomeVisitParticipant { HomeVisitId=visit.Id,ServantId=id });
        store.Add(visit); await Schedule(r.StudentId,r.NextVisitDate,"Visit",visit.Id);
        return ServiceResult<Guid>.Success(visit.Id);
    },r=>r.Status==ServiceResultStatus.Success);
    public Task<ServiceResult> UpdateVisit(Guid id,UpdateHomeVisitRequest r,Guid currentUserId) => store.Transaction(async () =>
    {
        var visit=await store.Query<HomeVisit>(true,true).Include(v=>v.Participants).SingleOrDefaultAsync(v=>v.Id==id, caller.CancellationToken);
        if (visit==null) return ServiceResult.Failure("Visit not found.");
        if (!caller.IsPrivileged && visit.ServantId!=caller.UserId) return ServiceResult.Failure("Not authorized.",new ServiceError("Forbidden"));
        if (visit.IsVoided) return ServiceResult.Failure("Visit voided.",new ServiceError("Conflict"));
        if (!VersionMatches(visit.Version,r.Version)) return ServiceResult.Failure("Visit changed. Reload before reviewing the correction.",new ServiceError("Conflict"));
        if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length>1000) return ServiceResult.Failure("A correction reason is required.");
        var error=await Validate(visit.StudentId,r.VisitDate,(int)r.VisitOutcome,typeof(YouthDataManager.Domain.Enums.VisitOutcome),r.Notes,r.NextVisitDate,r.ParticipantServantIds,true);
        if (error!=null) return Fail(error);
        Audit("HomeVisit",id,r.Reason!,new {visit.VisitDate,visit.VisitOutcome,visit.Notes,visit.NextVisitDate},new {r.VisitDate,r.VisitOutcome,r.Notes,r.NextVisitDate});
        store.Add(new AuditEntry { ActorId=caller.UserId,EntityType="HomeVisitParticipants",EntityId=id.ToString(),Action="Correction",Reason=r.Reason,CorrelationId=caller.CorrelationId,
            BeforeJson=System.Text.Json.JsonSerializer.Serialize(visit.Participants.Select(p=>p.ServantId)),AfterJson=System.Text.Json.JsonSerializer.Serialize((r.ParticipantServantIds ?? []).Append(visit.ServantId).Distinct()) });
        visit.VisitDate=r.VisitDate.ToUniversalTime(); visit.VisitOutcome=r.VisitOutcome; visit.Notes=r.Notes?.Trim() ?? ""; visit.NextVisitDate=r.NextVisitDate?.ToUniversalTime();
        var ids=(r.ParticipantServantIds ?? []).Append(visit.ServantId).Distinct().ToHashSet();
        foreach (var p in visit.Participants.Where(p=>!ids.Contains(p.ServantId)).ToArray()) store.Remove(p);
        foreach (var person in ids.Where(person=>!visit.Participants.Any(p=>p.ServantId==person))) visit.Participants.Add(new HomeVisitParticipant { HomeVisitId=id,ServantId=person });
        await CorrectSchedule(visit.StudentId,"HomeVisit",id,r.NextVisitDate,r.Reason!);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);

    private bool VersionMatches(byte[] current,string? input) => !string.IsNullOrWhiteSpace(input ?? caller.ExpectedVersion) && Convert.ToBase64String(current)==(input ?? caller.ExpectedVersion);
    private void Audit(string type,Guid id,string reason,object before,object after) => store.Add(new AuditEntry {
        ActorId=caller.UserId,EntityType=type,EntityId=id.ToString(),Action="Correction",Timestamp=caller.UtcNow,Reason=reason.Trim(),CorrelationId=caller.CorrelationId,
        BeforeJson=System.Text.Json.JsonSerializer.Serialize(before),AfterJson=System.Text.Json.JsonSerializer.Serialize(after) });
    private async Task CorrectSchedule(Guid studentId,string source,Guid id,DateTime? next,string reason)
    {
        var task=await store.Query<FollowUpTask>().SingleOrDefaultAsync(t=>t.SourceType==source && t.SourceId==id,caller.CancellationToken);
        if (task!=null && task.Status is "Open" or "PendingReview") {
            if (next.HasValue && !await store.Query<Student>(false).AnyAsync(s=>s.Id==studentId, caller.CancellationToken)) return;
            if (next.HasValue) task.DueAt=next.Value.ToUniversalTime(); else {task.Status="Cancelled";task.Reason=reason;}
        } else if (task==null) await Schedule(studentId,next,source=="CallLog" ? "Call" : "Visit",id);
    }
    public Task<ServiceResult> UpdateCall(Guid id,UpdateCallLogRequest r,Guid currentUserId) => store.Transaction(async () =>
    {
        var call=await store.Query<CallLog>(true,true).SingleOrDefaultAsync(c=>c.Id==id,caller.CancellationToken);
        if (call==null) return ServiceResult.Failure("Call not found.");
        if (!caller.IsPrivileged && call.ServantId!=caller.UserId) return ServiceResult.Failure("Not authorized.",new ServiceError("Forbidden"));
        if (call.IsVoided || !VersionMatches(call.Version,r.Version)) return ServiceResult.Failure("Call changed. Reload before reviewing the correction.",new ServiceError("Conflict"));
        if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length>1000) return ServiceResult.Failure("A correction reason (1–1000 characters) is required.");
        var error=await Validate(call.StudentId,r.CallDate,(int)r.CallStatus,typeof(YouthDataManager.Domain.Enums.CallStatus),r.Notes,r.NextFollowUpDate,historical:true);
        if (error!=null) return Fail(error);
        Audit("CallLog",id,r.Reason,new {call.CallDate,call.CallStatus,call.Notes,call.NextFollowUpDate},new {r.CallDate,r.CallStatus,r.Notes,r.NextFollowUpDate});
        call.CallDate=r.CallDate.ToUniversalTime();call.CallStatus=r.CallStatus;call.Notes=r.Notes?.Trim() ?? "";call.NextFollowUpDate=r.NextFollowUpDate?.ToUniversalTime();
        await CorrectSchedule(call.StudentId,"CallLog",id,r.NextFollowUpDate,r.Reason);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);
    public Task<ServiceResult> Void(Guid id,bool visit,VoidActivityRequest r) => store.Transaction(async () =>
    {
        if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length>1000) return ServiceResult.Failure("A void reason (1–1000 characters) is required.");
        var v=visit ? await store.Query<HomeVisit>(true,true).SingleOrDefaultAsync(v=>v.Id==id,caller.CancellationToken) : null;
        var c=visit ? null : await store.Query<CallLog>(true,true).SingleOrDefaultAsync(c=>c.Id==id,caller.CancellationToken);
        if (v==null && c==null) return ServiceResult.Failure("Activity not found.");
        var studentId=v?.StudentId ?? c!.StudentId; var actor=v?.ServantId ?? c!.ServantId;
        if (!caller.IsPrivileged && (actor!=caller.UserId || !await store.Query<Student>(false).AnyAsync(s=>s.Id==studentId && s.ServantId==caller.UserId, caller.CancellationToken))) return ServiceResult.Failure("Not authorized.",new ServiceError("Forbidden"));
        if (v?.IsVoided==true || c?.IsVoided==true) return ServiceResult.Success();
        if (!VersionMatches(v?.Version ?? c!.Version,r.Version)) return ServiceResult.Failure("Activity changed. Reload before reviewing the void.",new ServiceError("Conflict"));
        Audit(visit ? "HomeVisit" : "CallLog",id,r.Reason,new {IsVoided=false},new {IsVoided=true,VoidReason=r.Reason.Trim()});
        if (v!=null) {v.IsVoided=true;v.VoidReason=r.Reason.Trim();} else {c!.IsVoided=true;c.VoidReason=r.Reason.Trim();}
        await CorrectSchedule(studentId,visit ? "HomeVisit" : "CallLog",id,null,r.Reason);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);
}
