using YouthDataManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Exceptions;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Reports;

public partial class ReportDataProvider
{
    private static decimal? Rate(int present,int eligible) => eligible==0 ? null : Math.Round(100m*present/eligible,1);

    private async Task<ReportTable> ServantServiceTable(ReportSuiteFilter f,bool export)
    {
        if(f.Area!=null || f.College!=null || f.AcademicYear!=null || f.Gender.HasValue || f.BirthMonth.HasValue || f.HasServant.HasValue || f.Issue!=null || f.Status!=null || f.Type!=null)
            throw new DomainValidationException("هذا التقرير يقبل الفترة والخادم والبحث باسم الخادم فقط.");
        var now=_caller.UtcNow;var (start,end)=Period(f.DateFrom,f.DateTo);
        var calls=Calls.Where(c=>(!start.HasValue || c.CallDate>=start) && (!end.HasValue || c.CallDate<end));
        var visits=Visits.Where(v=>(!start.HasValue || v.VisitDate>=start) && (!end.HasValue || v.VisitDate<end));
        // A visit belongs to its recorder and distinct companions, including legacy visits with no companions.
        var credits=visits.Select(v=>new ServiceVisitCredit {VisitId=v.Id,StudentId=v.StudentId,ActorId=v.ServantId,Outcome=v.VisitOutcome})
            .Concat(from p in _context.HomeVisitParticipants.IgnoreQueryFilters().ForServiceYear(_context) join v in visits on p.HomeVisitId equals v.Id
                select new ServiceVisitCredit {VisitId=v.Id,StudentId=v.StudentId,ActorId=p.ServantId,Outcome=v.VisitOutcome}).Distinct();
        var callGroups=calls.GroupBy(c=>c.ServantId).Select(g=>new {Id=g.Key,Total=g.Count(),Answered=g.Count(c=>c.CallStatus==CallStatus.Answered),NotAnswered=g.Count(c=>c.CallStatus==CallStatus.NotAnswered),Busy=g.Count(c=>c.CallStatus==CallStatus.Busy),SwitchedOff=g.Count(c=>c.CallStatus==CallStatus.SwitchedOff),WrongNumber=g.Count(c=>c.CallStatus==CallStatus.WrongNumber)});
        var visitGroups=credits.GroupBy(v=>v.ActorId).Select(g=>new {Id=g.Key,Total=g.Count(),Successful=g.Count(v=>v.Outcome==VisitOutcome.Successful),NotAtHome=g.Count(v=>v.Outcome==VisitOutcome.NotAtHome),Refused=g.Count(v=>v.Outcome==VisitOutcome.RefusedVisit),Postponed=g.Count(v=>v.Outcome==VisitOutcome.Postponed)});
        var contacts=calls.Select(c=>new ServiceContact {ActorId=c.ServantId,StudentId=c.StudentId,Successful=c.CallStatus==CallStatus.Answered})
            .Concat(credits.Select(v=>new ServiceContact {ActorId=v.ActorId,StudentId=v.StudentId,Successful=v.Outcome==VisitOutcome.Successful}));
        var contactGroups=contacts.GroupBy(c=>c.ActorId).Select(g=>new {Id=g.Key,Attempted=g.Select(c=>c.StudentId).Distinct().Count(),Reached=g.Where(c=>c.Successful).Select(c=>c.StudentId).Distinct().Count()});
        var attendance=AttendanceEntries(f,"Servant",true).GroupBy(r=>r.PersonId).Select(g=>new {Id=g.Key,Eligible=g.Count(),Present=g.Count(r=>r.Present)});
        var assignments=Roster.Where(s=>s.ServantId!=null).GroupBy(s=>s.ServantId).Select(g=>new {Id=g.Key,Count=g.Count()});
        var tasks=_context.Set<FollowUpTask>().ForServiceYear(_context).AsNoTracking().Where(t=>t.AssigneeId!=null).GroupBy(t=>t.AssigneeId)
            .Select(g=>new {Id=g.Key,Open=g.Count(t=>t.Status=="Open"),Overdue=g.Count(t=>t.Status=="Open" && t.DueAt<now)});
        var roleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);
        // Select actor IDs before the aggregates instead of correlating aggregate subqueries per person.
        var actorIds=_context.Users.Where(u=>u.IsActive && _context.UserRoles.Any(r=>r.UserId==u.Id && roleIds.Contains(r.RoleId))).Select(u=>u.Id)
            .Union(calls.Select(c=>c.ServantId)).Union(credits.Select(v=>v.ActorId)).Union(AttendanceEntries(f,"Servant",true).Select(r=>r.PersonId))
            .Union(Roster.Where(s=>s.ServantId!=null).Select(s=>s.ServantId!.Value))
            .Union(_context.Set<FollowUpTask>().ForServiceYear(_context).Where(t=>t.Status=="Open" && t.AssigneeId!=null).Select(t=>t.AssigneeId!.Value));
        var people=_context.Users.AsNoTracking().Where(u=>actorIds.Contains(u.Id));
        if(f.ServantId.HasValue)people=people.Where(u=>u.Id==f.ServantId);
        if(!string.IsNullOrWhiteSpace(f.Search)){var term=ArabicNormalizer.Normalize(f.Search.Trim());people=people.Where(u=>u.NormalizedFullName.Contains(term) || u.FullName.Contains(f.Search.Trim()));}
        var q=from u in people
            join a in attendance on u.Id equals a.Id into attendanceRows from a in attendanceRows.DefaultIfEmpty()
            join c in callGroups on u.Id equals c.Id into callRows from c in callRows.DefaultIfEmpty()
            join v in visitGroups on u.Id equals v.Id into visitRows from v in visitRows.DefaultIfEmpty()
            join contact in contactGroups on u.Id equals contact.Id into contactRows from contact in contactRows.DefaultIfEmpty()
            join assigned in assignments on (Guid?)u.Id equals assigned.Id into assignmentRows from assigned in assignmentRows.DefaultIfEmpty()
            join task in tasks on (Guid?)u.Id equals task.Id into taskRows from task in taskRows.DefaultIfEmpty()
            select new ServiceSummaryRow {Id=u.Id,Name=u.FullName,Active=u.IsActive,AssignedStudentsCount=(int?)assigned.Count ?? 0,
                Eligible=(int?)a.Eligible ?? 0,Present=(int?)a.Present ?? 0,Calls=(int?)c.Total ?? 0,CallAnswered=(int?)c.Answered ?? 0,CallNotAnswered=(int?)c.NotAnswered ?? 0,CallBusy=(int?)c.Busy ?? 0,CallSwitchedOff=(int?)c.SwitchedOff ?? 0,CallWrongNumber=(int?)c.WrongNumber ?? 0,
                Visits=(int?)v.Total ?? 0,VisitSuccessful=(int?)v.Successful ?? 0,VisitNotAtHome=(int?)v.NotAtHome ?? 0,VisitRefusedVisit=(int?)v.Refused ?? 0,VisitPostponed=(int?)v.Postponed ?? 0,
                Attempted=(int?)contact.Attempted ?? 0,Reached=(int?)contact.Reached ?? 0,OpenTasks=(int?)task.Open ?? 0,OverdueTasks=(int?)task.Overdue ?? 0};
        var totals=await q.GroupBy(_=>1).Select(g=>new {Count=g.Count(),Eligible=g.Sum(r=>r.Eligible),Present=g.Sum(r=>r.Present),Calls=g.Sum(r=>r.Calls),VisitCredits=g.Sum(r=>r.Visits),Open=g.Sum(r=>r.OpenTasks),Overdue=g.Sum(r=>r.OverdueTasks)}).SingleOrDefaultAsync(Ct);
        var selected=people.Select(u=>u.Id);
        var globalVisits=await credits.Where(v=>selected.Contains(v.ActorId)).Select(v=>v.VisitId).Distinct().CountAsync(Ct);
        return await Table("ملخص خدمة الخدام",f,export,q.OrderBy(r=>r.Name).ThenBy(r=>r.Id),[
            new("name","الخادم"),new("active","الحساب نشط","boolean"),new("assignedStudentsCount","مخدوموه الحاليون","number"),new("eligible","اجتماعات في قائمة حضوره","number"),new("present","حضر","number"),new("rate","نسبة حضور الخادم %","percent"),
            new("calls","المكالمات","number"),new("callAnswered","مجابة","number"),new("callNotAnswered","بلا رد","number"),new("callBusy","مشغول","number"),new("callSwitchedOff","الهاتف مغلق","number"),new("callWrongNumber","رقم خاطئ","number"),
            new("visits","زيارات شارك فيها","number"),new("visitSuccessful","زيارات ناجحة","number"),new("visitNotAtHome","المخدوم غير موجود","number"),new("visitRefusedVisit","زيارات مرفوضة","number"),new("visitPostponed","زيارات مؤجلة","number"),
            new("attempted","مخدومون حاول افتقادهم","number"),new("reached","مخدومون تواصل معهم بنجاح","number"),new("openTasks","متابعات مفتوحة الآن","number"),new("overdueTasks","متابعات متأخرة الآن","number")],
            new(){["owners"]=totals?.Count ?? 0,["eligible"]=totals?.Eligible ?? 0,["present"]=totals?.Present ?? 0,["rate"]=Rate(totals?.Present ?? 0,totals?.Eligible ?? 0),["calls"]=totals?.Calls ?? 0,["visits"]=globalVisits,["visitCredits"]=totals?.VisitCredits ?? 0,["openTasks"]=totals?.Open ?? 0,["overdueTasks"]=totals?.Overdue ?? 0},totals?.Count ?? 0);
    }

    private async Task<ReportTable> GroupAttendanceTable(ReportSuiteFilter f,bool export)
    {
        if(f.Status!=null || f.Type!=null)throw new DomainValidationException("Invalid group filter.");
        if(f.Issue!=null && f.Issue!="unassigned-group")throw new DomainValidationException("Invalid group filter.");
        var groups=AttendanceEntries(f,"Student",true).GroupBy(r=>r.OwnerId ?? Guid.Empty)
            .Select(g=>new {Id=g.Key,Students=g.Select(r=>r.PersonId).Distinct().Count(),Eligible=g.Count(),Present=g.Count(r=>r.Present)});
        var q=from g in groups join u in _context.Users.AsNoTracking() on g.Id equals u.Id into users from u in users.DefaultIfEmpty()
            select new GroupAttendanceRow {Id=g.Id,Name=u==null ? "بلا خادم وقت الاجتماع" : u.FullName,Students=g.Students,Eligible=g.Eligible,Present=g.Present,Absent=g.Eligible-g.Present};
        var total=await q.GroupBy(_=>1).Select(g=>new {Count=g.Count(),Eligible=g.Sum(r=>r.Eligible),Present=g.Sum(r=>r.Present)}).SingleOrDefaultAsync(Ct);
        return await Table("حضور مجموعات الخدام",f,export,q.OrderBy(r=>r.Name).ThenBy(r=>r.Id),[
            new("name","خادم المجموعة وقت الاجتماع"),new("students","مخدومون مختلفون في القوائم","number"),new("eligible","فرص حضور مؤكدة","number"),new("present","مرات الحضور","number"),new("absent","مرات الغياب","number"),new("rate","نسبة حضور المجموعة %","percent")],
            new(){["owners"]=total?.Count ?? 0,["eligible"]=total?.Eligible ?? 0,["present"]=total?.Present ?? 0,["absent"]=(total?.Eligible ?? 0)-(total?.Present ?? 0),["rate"]=Rate(total?.Present ?? 0,total?.Eligible ?? 0)},total?.Count ?? 0);
    }

    private async Task<ReportTable> PastoralFollowUpTable(ReportSuiteFilter f,bool export)
    {
        if(f.Status!=null || f.Type!=null)throw new DomainValidationException("Invalid pastoral filter.");
        var now=_caller.UtcNow;var roster=SuiteRoster(f);
        var asOf=f.DateTo!.Value.Date;if(_caller.SelectedServiceYear is { } year && asOf>year.AsOf(now))asOf=year.AsOf(now);var end=BusinessTime.UtcStart(asOf.AddDays(1));var cutoff=BusinessTime.UtcStart(asOf.AddDays(-f.Days));
        // Seek the person's indexed history rather than repeatedly aggregating every group's history.
        // Current ownership selects the roster; historical meeting ownership never changes eligibility.
        var entries=from r in _context.Set<MeetingRosterEntry>().AsNoTracking() join m in _context.Set<MeetingSession>().ForServiceYear(_context).AsNoTracking() on r.SessionId equals m.Id
            where r.Kind=="Student" && m.Status=="Finalized" && m.RosterKnown && m.Date<=asOf
            select new {r.PersonId,r.Present,m.Date};
        var periodEntries=entries.Where(r=>r.Date>=f.DateFrom!.Value.Date);
        var contacts=Calls.Where(c=>c.CallDate<end).Select(c=>new ContactEvent {StudentId=c.StudentId,At=c.CallDate,Successful=c.CallStatus==CallStatus.Answered})
            .Concat(Visits.Where(v=>v.VisitDate<end).Select(v=>new ContactEvent {StudentId=v.StudentId,At=v.VisitDate,Successful=v.VisitOutcome==VisitOutcome.Successful}));
        var tasks=_context.Set<FollowUpTask>().ForServiceYear(_context).AsNoTracking().Where(t=>t.Status=="Open");
        var measurements=roster.Select(s=>new {
            s.Id,s.FullName,s.ServantId,Owner=s.Servant==null ? "غير موزع على خادم" : s.Servant.FullName,s.Phone,
            Eligible=periodEntries.Count(r=>r.PersonId==s.Id),Present=periodEntries.Count(r=>r.PersonId==s.Id && r.Present),
            ConsecutiveAbsences=periodEntries.Count(r=>r.PersonId==s.Id && !r.Present && !periodEntries.Any(p=>p.PersonId==s.Id && p.Present && p.Date>r.Date)),
            LastPresent=entries.Where(r=>r.PersonId==s.Id && r.Present).Select(r=>(DateTime?)r.Date).Max(),
            LastAttempt=contacts.Where(c=>c.StudentId==s.Id).Select(c=>(DateTime?)c.At).Max(),
            LastSuccess=contacts.Where(c=>c.StudentId==s.Id && c.Successful).Select(c=>(DateTime?)c.At).Max(),
            OpenTasks=tasks.Count(t=>t.StudentId==s.Id),OverdueTasks=tasks.Count(t=>t.StudentId==s.Id && t.DueAt<now)});
        var q=measurements.Select(s=>new PastoralRow {Id=s.Id,Name=s.FullName,OwnerId=s.ServantId,Owner=s.Owner,Phone=s.Phone,
            Eligible=s.Eligible,Present=s.Present,ConsecutiveAbsences=s.ConsecutiveAbsences,LastPresent=s.LastPresent,
            LastAttempt=s.LastAttempt,LastSuccess=s.LastSuccess,OpenTasks=s.OpenTasks,OverdueTasks=s.OverdueTasks,
            AttendanceUnknown=s.Eligible==0,NeedsAttendance=s.ConsecutiveAbsences>=f.MinConsecutiveAbsences,NoRecentSuccess=s.LastSuccess==null || s.LastSuccess<cutoff,NeverAttempted=s.LastAttempt==null});
        q=q.Where(r=>r.AttendanceUnknown || r.NeedsAttendance || r.NoRecentSuccess || r.OverdueTasks>0);
        if(f.Issue!=null)q=f.Issue switch {
            "consecutive-absence"=>q.Where(r=>r.NeedsAttendance),"no-recent-success"=>q.Where(r=>r.NoRecentSuccess),"never-attempted"=>q.Where(r=>r.NeverAttempted),
            "attendance-unknown"=>q.Where(r=>r.AttendanceUnknown),"overdue-tasks"=>q.Where(r=>r.OverdueTasks>0),_=>throw new DomainValidationException("Invalid pastoral filter.")};
        // Distinct IDs force a derived table: SQL Server cannot aggregate an expression containing a scalar subquery.
        var totals=await q.Select(r=>new {r.Id,r.NeedsAttendance,r.AttendanceUnknown,r.NoRecentSuccess,r.NeverAttempted,r.OverdueTasks}).Distinct().GroupBy(_=>1).Select(g=>new {Count=g.Count(),Absent=g.Count(r=>r.NeedsAttendance),Unknown=g.Count(r=>r.AttendanceUnknown),NoSuccess=g.Count(r=>r.NoRecentSuccess),Never=g.Count(r=>r.NeverAttempted),Overdue=g.Count(r=>r.OverdueTasks>0)}).SingleOrDefaultAsync(Ct);
        return await Table("مخدومون يحتاجون افتقادًا",f,export,q.OrderByDescending(r=>r.ConsecutiveAbsences).ThenBy(r=>r.Name).ThenBy(r=>r.Id),[
            new("name","المخدوم"),new("owner","خادمه الحالي"),new("phone","الهاتف","phone"),new("reason","سبب المتابعة"),new("consecutiveAbsences","غياب متتالي في الفترة","number"),
            new("eligible","فرص حضور مؤكدة في الفترة","number"),new("present","مرات الحضور في الفترة","number"),new("rate","نسبة الحضور %","percent"),new("lastPresent","آخر حضور مؤكد حتى نهاية الفترة","date"),
            new("lastAttempt","آخر محاولة افتقاد حتى نهاية الفترة","datetime"),new("lastSuccess","آخر تواصل ناجح حتى نهاية الفترة","datetime"),new("openTasks","متابعات مفتوحة الآن","number"),new("overdueTasks","متابعات متأخرة الآن","number")],
            new(){["roster"]=totals?.Count ?? 0,["consecutiveAbsenceStudents"]=totals?.Absent ?? 0,["unknownAttendanceStudents"]=totals?.Unknown ?? 0,["noRecentSuccess"]=totals?.NoSuccess ?? 0,["neverAttempted"]=totals?.Never ?? 0,["overdueStudents"]=totals?.Overdue ?? 0},totals?.Count ?? 0);
    }

    private sealed class ServiceVisitCredit {public Guid VisitId {get;init;}public Guid StudentId {get;init;}public Guid ActorId {get;init;}public VisitOutcome Outcome {get;init;} }
    private sealed class ServiceContact {public Guid ActorId {get;init;}public Guid StudentId {get;init;}public bool Successful {get;init;} }
    private sealed class ServiceSummaryRow
    {
        public Guid Id {get;init;}public string Name {get;init;}="";public bool Active {get;init;}public int AssignedStudentsCount {get;init;}
        public int Eligible {get;init;}public int Present {get;init;}public int Calls {get;init;}public int Visits {get;init;}
        public int CallAnswered {get;init;}public int CallNotAnswered {get;init;}public int CallBusy {get;init;}public int CallSwitchedOff {get;init;}public int CallWrongNumber {get;init;}
        public int VisitSuccessful {get;init;}public int VisitNotAtHome {get;init;}public int VisitRefusedVisit {get;init;}public int VisitPostponed {get;init;}
        public int Attempted {get;init;}public int Reached {get;init;}public int OpenTasks {get;init;}public int OverdueTasks {get;init;}
    }
    private sealed class GroupAttendanceRow {public Guid Id {get;init;}public string Name {get;init;}="";public int Students {get;init;}public int Eligible {get;init;}public int Present {get;init;}public int Absent {get;init;} }
    private sealed class PastoralRow
    {
        public Guid Id {get;init;}public string Name {get;init;}="";public Guid? OwnerId {get;init;}public string Owner {get;init;}="";public string Phone {get;init;}="";
        public int Eligible {get;init;}public int Present {get;init;}public int ConsecutiveAbsences {get;init;}public DateTime? LastPresent {get;init;}public DateTime? LastAttempt {get;init;}public DateTime? LastSuccess {get;init;}
        public int OpenTasks {get;init;}public int OverdueTasks {get;init;}public bool AttendanceUnknown {get;init;}public bool NeedsAttendance {get;init;}public bool NoRecentSuccess {get;init;}public bool NeverAttempted {get;init;}
        public string Reason => string.Join("، ",new[]{NeedsAttendance ? "غياب متتالي" : null,AttendanceUnknown ? "بيانات حضور غير كافية في الفترة" : null,NeverAttempted ? "لا توجد محاولة افتقاد مسجلة" : NoRecentSuccess ? "لا يوجد تواصل ناجح حديث" : null,OverdueTasks>0 ? "متابعة متأخرة" : null}.Where(s=>s!=null));
    }
}
