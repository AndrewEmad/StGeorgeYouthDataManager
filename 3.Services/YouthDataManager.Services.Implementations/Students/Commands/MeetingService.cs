using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;

namespace YouthDataManager.Students.Service.Implementations.Commands;

public sealed class MeetingService(IApplicationStore store, ICallerContext? caller=null) : IMeetingService
{
    private CancellationToken Ct=>caller?.CancellationToken ?? default;
    public Task<ServiceResult<MeetingSession>> Start(DateTime date) => store.Transaction(async () =>
    {
        date=date.Date;
        if (date.Year<2000 || date>BusinessTime.LocalNow.Date.AddYears(1)) return ServiceResult<MeetingSession>.Failure("Invalid meeting date.");
        var existing=await store.Query<MeetingSession>().SingleOrDefaultAsync(s => s.Date==date,Ct);
        if (existing!=null) return ServiceResult<MeetingSession>.Success(existing);
        var eligible=EligibleUsers.Query(store);var priorStudents=store.Query<StudentAttendance>(false,true);var priorServants=store.Query<ServantAttendance>(false);
        var students=await store.Query<Student>(false,true).Where(s=>!s.IsDeleted || priorStudents.Any(a=>a.Date==date && a.StudentId==s.Id)).Select(s => new { s.Id,s.FullName,s.ServantId }).ToListAsync(Ct);
        var servants=await store.Query<ApplicationUser>(false).Where(u=>eligible.Any(e=>e.Id==u.Id) || priorServants.Any(a=>a.Date==date && a.ServantId==u.Id)).Select(u => new {u.Id,u.FullName}).ToListAsync(Ct);
        var legacyStudents=await store.Query<StudentAttendance>(false,true).Where(a => a.Date==date).Select(a=>a.StudentId).ToListAsync(Ct);
        var legacyServants=await store.Query<ServantAttendance>(false).Where(a=>a.Date==date).Select(a=>a.ServantId).ToListAsync(Ct);
        var session=new MeetingSession { Date=date,RosterKnown=legacyStudents.Count==0 && legacyServants.Count==0 };
        store.Add(session);
        foreach (var s in students) store.Add(new MeetingRosterEntry { SessionId=session.Id,PersonId=s.Id,Name=s.FullName,AssignedServantId=s.ServantId,Present=legacyStudents.Contains(s.Id) });
        foreach (var u in servants) store.Add(new MeetingRosterEntry { SessionId=session.Id,PersonId=u.Id,Name=u.FullName,Kind="Servant",Present=legacyServants.Contains(u.Id) });
        return ServiceResult<MeetingSession>.Success(session);
    },r=>r.Status==ServiceResultStatus.Success);

    public Task<ServiceResult> Mark(Guid sessionId, string kind, IReadOnlyList<Guid> ids, bool present, string? reason) => store.Transaction(async () =>
    {
        if (kind is not ("Student" or "Servant") || ids.Count==0 || ids.Count>5000 || ids.Any(id=>id==Guid.Empty)) return ServiceResult.Failure("Invalid roster selection.");
        var session=await store.Query<MeetingSession>().SingleOrDefaultAsync(s=>s.Id==sessionId,Ct);
        if (session==null) return ServiceResult.Failure("Meeting not found.");
        if (session.Status=="Cancelled") return ServiceResult.Failure("Meeting cancelled.",new ServiceError("Conflict"));
        if (session.Status=="Finalized" && string.IsNullOrWhiteSpace(reason)) return ServiceResult.Failure("A correction reason is required.");
        var distinct=ids.Distinct().ToArray();
        var rows=await store.Query<MeetingRosterEntry>().Where(r=>r.SessionId==sessionId && r.Kind==kind && distinct.Contains(r.PersonId)).ToListAsync(Ct);
        if (rows.Count!=distinct.Length) return ServiceResult.Failure("Selection is outside the snapshotted roster.",new ServiceError("Conflict"));
        foreach (var r in rows) r.Present=present;
        // Touch the rowversion so simultaneous corrections cannot bypass session concurrency.
        session.UpdatedAt=DateTime.UtcNow;session.Reason=reason?.Trim();
        if (session.Status=="Finalized") await Reconcile(session);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);

    public Task<ServiceResult> ChangeStatus(Guid id,string status,string? reason) => store.Transaction(async () =>
    {
        if (status is not ("Finalized" or "Cancelled")) return ServiceResult.Failure("Invalid meeting status.");
        var session=await store.Query<MeetingSession>().SingleOrDefaultAsync(s=>s.Id==id,Ct);
        if (session==null) return ServiceResult.Failure("Meeting not found.");
        if (session.Status==status) return ServiceResult.Success();
        if (session.Status!="Draft" && status!="Cancelled") return ServiceResult.Failure("Meeting already processed.",new ServiceError("Conflict"));
        if (status=="Cancelled" && string.IsNullOrWhiteSpace(reason)) return ServiceResult.Failure("Cancellation reason is required.");
        session.Status=status; session.FinalizedAt=DateTime.UtcNow;session.UpdatedAt=DateTime.UtcNow;session.Reason=reason?.Trim();
        await Reconcile(session);
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);

    public Task<ServiceResult> ReviewEligibility(Guid id,IReadOnlyList<Guid> students,IReadOnlyList<Guid> servants,string reason) => store.Transaction(async()=>
    {
        if(string.IsNullOrWhiteSpace(reason) || reason.Trim().Length>2000 || students.Count>5000 || servants.Count>5000 || students.Concat(servants).Any(x=>x==Guid.Empty))return ServiceResult.Failure("Valid eligibility evidence and selections are required.");
        var session=await store.Query<MeetingSession>().SingleOrDefaultAsync(s=>s.Id==id,Ct);
        if(session==null)return ServiceResult.Failure("Meeting not found.");
        if(session.RosterKnown || session.Status=="Cancelled")return ServiceResult.Failure("This historical roster cannot be reviewed.",new ServiceError("Conflict"));
        var ids=students.Distinct().ToArray();var userIds=servants.Distinct().ToArray();
        var studentNames=await store.Query<Student>(false,true).Where(s=>ids.Contains(s.Id)).ToDictionaryAsync(s=>s.Id,s=>s.FullName);
        var userNames=await store.Query<ApplicationUser>(false).Where(u=>userIds.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.FullName);
        if(studentNames.Count!=ids.Length || userNames.Count!=userIds.Length)return ServiceResult.Failure("An eligible person no longer exists.");
        var rows=await store.Query<MeetingRosterEntry>().Where(r=>r.SessionId==id).ToListAsync(Ct);
        if(rows.Any(r=>r.Present && !(r.Kind=="Student" ? ids : userIds).Contains(r.PersonId)))return ServiceResult.Failure("Historical positive attendance must be preserved.");
        foreach(var row in rows)
        {
            if(!(row.Kind=="Student" ? ids : userIds).Contains(row.PersonId))store.Remove(row);
            // Historical assignment cannot be inferred from today's student owner.
            else row.AssignedServantId=null;
        }
        foreach(var student in studentNames.Where(s=>!rows.Any(r=>r.Kind=="Student" && r.PersonId==s.Key)))store.Add(new MeetingRosterEntry {SessionId=id,PersonId=student.Key,Name=student.Value});
        foreach(var user in userNames.Where(u=>!rows.Any(r=>r.Kind=="Servant" && r.PersonId==u.Key)))store.Add(new MeetingRosterEntry {SessionId=id,PersonId=user.Key,Name=user.Value,Kind="Servant"});
        session.RosterKnown=true;session.Reason=reason.Trim();session.UpdatedAt=DateTime.UtcNow;
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);

    private async Task Reconcile(MeetingSession session)
    {
        var roster=await store.Query<MeetingRosterEntry>().Where(r=>r.SessionId==session.Id).ToListAsync(Ct);
        foreach (var a in await store.Query<StudentAttendance>(true,true).Where(a=>a.Date==session.Date).ToListAsync(Ct)) store.Remove(a);
        foreach (var a in await store.Query<ServantAttendance>().Where(a=>a.Date==session.Date).ToListAsync(Ct)) store.Remove(a);
        await store.Save();
        if (session.Status=="Finalized")
            foreach (var r in roster.Where(r=>r.Present))
                if (r.Kind=="Student") store.Add(new StudentAttendance { Id=Guid.NewGuid(),StudentId=r.PersonId,Date=session.Date });
                else store.Add(new ServantAttendance { Id=Guid.NewGuid(),ServantId=r.PersonId,Date=session.Date });
        await store.Save();
        var ids=roster.Where(r=>r.Kind=="Student").Select(r=>r.PersonId).ToArray();
        var latest=await store.Query<StudentAttendance>(false,true).Where(a=>ids.Contains(a.StudentId)).GroupBy(a=>a.StudentId).Select(g=>new {Id=g.Key,Date=g.Max(a=>a.Date)}).ToDictionaryAsync(a=>a.Id,a=>a.Date);
        foreach (var s in await store.Query<Student>(true,true).Where(s=>ids.Contains(s.Id)).ToListAsync(Ct)) s.LastAttendanceDate=latest.TryGetValue(s.Id,out var date) ? date : null;
    }
}
