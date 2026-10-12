using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Data;

public sealed class LegacyBackfill(IApplicationStore store)
{
    public async Task<object> Run(bool dryRun,int size,CancellationToken ct)
    {
        if(size is <1 or >500)throw new YouthDataManager.Domain.Exceptions.DomainValidationException("Batch size must be 1-500.");
        var tasks=store.Query<FollowUpTask>(false);var sessions=store.Query<MeetingSession>(false);
        var calls=store.Query<CallLog>(false,true).Where(c=>!c.IsVoided && c.NextFollowUpDate!=null && !tasks.Any(t=>t.SourceType=="CallLog" && t.SourceId==c.Id));
        var visits=store.Query<HomeVisit>(false,true).Where(v=>!v.IsVoided && v.NextVisitDate!=null && !tasks.Any(t=>t.SourceType=="HomeVisit" && t.SourceId==v.Id));
        var dates=store.Query<StudentAttendance>(false,true).Select(a=>a.Date).Union(store.Query<ServantAttendance>(false,true).Select(a=>a.Date)).Where(date=>!sessions.Any(s=>s.Date==date)).Distinct();
        var birthdays=store.Query<Student>(true,true).Where(s=>s.BirthDate!=null && s.BirthMonth!=s.BirthDate.Value.Month);
        var remaining=new {Calls=await calls.CountAsync(ct),Visits=await visits.CountAsync(ct),AttendanceDates=await dates.CountAsync(ct),Birthdays=await birthdays.CountAsync(ct)};
        if(dryRun)return new {DryRun=true,Remaining=remaining};
        return await store.Transaction<object>(async()=>{
            var eligible=EligibleUsers.Query(store);var students=store.Query<Student>(false,true);
            var callBatch=await calls.OrderBy(c=>c.Id).Take(size).ToListAsync(ct);var visitBatch=await visits.OrderBy(v=>v.Id).Take(size).ToListAsync(ct);
            var ids=callBatch.Select(c=>c.StudentId).Concat(visitBatch.Select(v=>v.StudentId)).Distinct().ToArray();
            var owners=await students.Where(s=>ids.Contains(s.Id) && !s.IsDeleted && s.ServantId!=null && eligible.Any(u=>u.Id==s.ServantId)).ToDictionaryAsync(s=>s.Id,s=>s.ServantId,ct);
            var dateBatch=await dates.OrderBy(d=>d).Take(size).ToListAsync(ct);
            var positiveStudents=await store.Query<StudentAttendance>(false,true).Where(a=>dateBatch.Contains(a.Date)).Select(a=>new {a.Date,a.StudentId,a.Student.FullName}).Distinct().ToListAsync(ct);
            var positiveServants=await store.Query<ServantAttendance>(false,true).Where(a=>dateBatch.Contains(a.Date)).Select(a=>new {a.Date,a.ServantId,a.Servant.FullName}).Distinct().ToListAsync(ct);
            var processed=0;
            foreach(var c in callBatch)
            {
                var owner=owners.GetValueOrDefault(c.StudentId);
                store.Add(new FollowUpTask {StudentId=c.StudentId,AssigneeId=owner,Type="Call",Status="PendingReview",DueAt=c.NextFollowUpDate!.Value,SourceType="CallLog",SourceId=c.Id,Reason="Legacy scheduled date requires review"});processed++;
            }
            foreach(var v in visitBatch)
            {
                var owner=owners.GetValueOrDefault(v.StudentId);
                store.Add(new FollowUpTask {StudentId=v.StudentId,AssigneeId=owner,Type="Visit",Status="PendingReview",DueAt=v.NextVisitDate!.Value,SourceType="HomeVisit",SourceId=v.Id,Reason="Legacy scheduled date requires review"});processed++;
            }
            foreach(var date in dateBatch)
            {
                var session=new MeetingSession {Date=date.Date,Status="Finalized",RosterKnown=false,Reason="Legacy positive attendance; historical eligible roster is unknown"};store.Add(session);
                foreach(var s in positiveStudents.Where(s=>s.Date==date))store.Add(new MeetingRosterEntry {SessionId=session.Id,PersonId=s.StudentId,Name=s.FullName,Present=true});
                foreach(var u in positiveServants.Where(u=>u.Date==date))store.Add(new MeetingRosterEntry {SessionId=session.Id,PersonId=u.ServantId,Name=u.FullName,Kind="Servant",Present=true});
                processed++;
            }
            foreach(var s in await birthdays.OrderBy(s=>s.Id).Take(size).ToListAsync(ct)){s.BirthMonth=s.BirthDate!.Value.Month;processed++;}
            await store.Save(ct);return new {DryRun=false,Processed=processed,RemainingBeforeBatch=remaining};
        });
    }
}
