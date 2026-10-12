using System.Reflection;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Exceptions;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Reports;

public partial class ReportDataProvider
{
    public async Task<ReportTable> Suite(string report,ReportSuiteFilter f,bool export=false)
    {
        Page(f.Page,f.PageSize,null);
        f.Status=string.IsNullOrWhiteSpace(f.Status) ? null : f.Status;f.Type=string.IsNullOrWhiteSpace(f.Type) ? null : f.Type;f.Issue=string.IsNullOrWhiteSpace(f.Issue) ? null : f.Issue;
        if(f.Days is <0 or >36500 || f.ServantId==Guid.Empty) throw new DomainValidationException("Invalid report filters.");
        if(!_caller.IsPrivileged && f.ServantId.HasValue && f.ServantId!=_caller.UserId) throw new DomainValidationException("Invalid caller scope.");
        _=Period(f.DateFrom,f.DateTo);
        return report switch {
            "contact" => await ContactTable(f,export),
            "follow-up" => await TaskTable(f,export),
            "workload" => await WorkloadTable(f,export),
            "workload-owners" => await OwnerWorkloadTable(f,export),
            "approvals" => await ApprovalTable(f,export),
            "data-quality" => await QualityTable(f,export),
            "attendance" or "servant-attendance" => await ConcurrentReportReads.Run(
                _context.Database.GetDbConnection().DataSource+"|"+_context.Database.GetDbConnection().Database+"|"+report+"|"+(_caller.IsPrivileged ? "global" : _caller.UserId.ToString())+"|"+export+"|"+System.Text.Json.JsonSerializer.Serialize(f),
                ()=>AttendanceTable(report,f,export),Ct),
            "attendance-history" or "servant-attendance-history" => await AttendanceHistoryTable(report,f,export),
            "meeting-trends" => await MeetingTable(f,export),
            _ => throw new DomainValidationException("Unknown report.") };
    }
    private IQueryable<Student> SuiteRoster(ReportSuiteFilter f)
    {
        var q=StudentFilter(f.StudentFilter);
        if(f.ServantId.HasValue)q=q.Where(s=>s.ServantId==f.ServantId);
        return q;
    }
    private async Task<ReportTable> Table<T>(string title,ReportSuiteFilter f,bool export,IQueryable<T> ordered,ReportColumn[] columns,Dictionary<string,decimal?>? totals=null,int? totalCount=null,List<T>? loadedRows=null)
    {
        var count=totalCount ?? await ordered.CountAsync(Ct);
        if(export) ExportBound(count);
        var rows=loadedRows ?? await (export ? ordered.Take(_exportLimit) : ordered.Skip((f.Page-1)*f.PageSize).Take(f.PageSize)).ToListAsync(Ct);
        var properties=typeof(T).GetProperties().ToDictionary(p=>char.ToLowerInvariant(p.Name[0])+p.Name[1..]);
        var mapped=rows.Select(row=>columns.ToDictionary(c=>c.Key,c=>properties.TryGetValue(c.Key,out var p) ? p.GetValue(row) : null)).ToList();
        foreach(var row in mapped)
        {
            if(row.TryGetValue("eligible",out var d) && d is int denominator && row.TryGetValue("present",out var n) && n is int numerator)
                row["rate"]=denominator==0 || (row.TryGetValue("rosterKnown",out var known) && known is false) ? null : Math.Round(100m*numerator/denominator,1);
        }
        var scopeFilters=new List<string>();
        if(!string.IsNullOrWhiteSpace(f.Search))scopeFilters.Add("بحث: "+f.Search.Trim());
        if(f.Area!=null)scopeFilters.Add("المنطقة: "+(f.Area=="__missing__" ? "غير مسجلة" : f.Area));
        if(f.AcademicYear!=null)scopeFilters.Add("السنة: "+(f.AcademicYear=="__missing__" ? "غير مسجلة" : f.AcademicYear));
        if(f.BirthMonth.HasValue)scopeFilters.Add("شهر الميلاد: "+(f.BirthMonth==0 ? "غير مسجل" : f.BirthMonth.ToString()));
        if(f.ServantId.HasValue)scopeFilters.Add("المسؤول / مقدم الطلب: "+await _context.Users.Where(u=>u.Id==f.ServantId).Select(u=>u.FullName).SingleOrDefaultAsync(Ct));
        if(f.Status!=null)scopeFilters.Add("الحالة: "+ReportLabels.Arabic(f.Status));if(f.Type!=null)scopeFilters.Add("النوع: "+ReportLabels.Arabic(f.Type));if(f.Issue!=null)scopeFilters.Add("حالة البيانات: "+ReportLabels.Arabic(f.Issue));
        scopeFilters.Add("حد الاتصال الحديث / عمر الطلب: "+f.Days+" يومًا");
        var scope=(export ? "كل الصفوف المطابقة للفلاتر" : "الصفحة الحالية؛ الإجماليات لكل النتائج")+" · "+string.Join(" · ",scopeFilters);
        var period=$"{f.DateFrom?.ToString("yyyy-MM-dd") ?? "كل التواريخ"} – {f.DateTo?.ToString("yyyy-MM-dd") ?? "بلا حد نهائي"} (القاهرة)";
        return new(title,period,scope,columns,mapped,count,export ? 1 : f.Page,export ? count : f.PageSize,totals ?? new(){["total"]=count});
    }
    private async Task<ReportTable> ContactTable(ReportSuiteFilter f,bool export)
    {
        var roster=SuiteRoster(f);var (start,end)=Period(f.DateFrom,f.DateTo);var roleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);var eligibleIds=_context.Users.Where(u=>u.IsActive && _context.UserRoles.Any(r=>r.UserId==u.Id && roleIds.Contains(r.RoleId))).Select(u=>u.Id);
        var calls=_context.CallLogs.IgnoreQueryFilters().AsNoTracking().Where(c=>!c.IsVoided);
        var visits=_context.HomeVisits.IgnoreQueryFilters().AsNoTracking().Where(v=>!v.IsVoided);
        var periodCalls=calls.Where(c=>(!start.HasValue || c.CallDate>=start) && (!end.HasValue || c.CallDate<end));
        var periodVisits=visits.Where(v=>(!start.HasValue || v.VisitDate>=start) && (!end.HasValue || v.VisitDate<end));
        var cutoff=BusinessTime.UtcStart(TimeZoneInfo.ConvertTimeFromUtc(_caller.UtcNow,BusinessTime.Zone).Date.AddDays(-f.Days));
        var events=calls.Select(c=>new ContactEvent {StudentId=c.StudentId,At=c.CallDate,Successful=c.CallStatus==CallStatus.Answered})
            .Concat(visits.Select(v=>new ContactEvent {StudentId=v.StudentId,At=v.VisitDate,Successful=v.VisitOutcome==VisitOutcome.Successful}));
        var history=events.GroupBy(e=>e.StudentId).Select(g=>new {StudentId=g.Key,LastAttempt=g.Max(e=>e.At),LastSuccess=g.Max(e=>e.Successful ? (DateTime?)e.At : null),
            Attempts=g.Sum(e=>(!start.HasValue || e.At>=start) && (!end.HasValue || e.At<end) ? 1 : 0),
            Reached=g.Max(e=>e.Successful && (!start.HasValue || e.At>=start) && (!end.HasValue || e.At<end) ? 1 : 0)>0});
        var q=(from student in roster join last in history on student.Id equals last.StudentId into historyRows from last in historyRows.DefaultIfEmpty()
            select new ContactRow {Id=student.Id,Name=student.FullName,Owner=student.Servant==null ? "غير مسند" : student.Servant.FullName,
                Attempts=(int?)last.Attempts ?? 0,Reached=(bool?)last.Reached ?? false,LastAttempt=(DateTime?)last.LastAttempt,LastSuccess=last.LastSuccess}).TagWith("YDM:ContactCoverage");
        if(f.Issue!=null)q=f.Issue switch {"never-attempted"=>q.Where(r=>r.LastAttempt==null),"no-recent-attempt"=>q.Where(r=>r.LastAttempt==null || r.LastAttempt<cutoff),"no-recent-success"=>q.Where(r=>r.LastSuccess==null || r.LastSuccess<cutoff),_=>throw new DomainValidationException("Invalid contact filter.")};
        var aggregate=await q.GroupBy(_=>1).Select(g=>new {Population=g.Count(),Attempted=g.Count(r=>r.Attempts>0),Reached=g.Count(r=>r.Reached),Never=g.Count(r=>r.LastAttempt==null),NoAttempt=g.Count(r=>r.LastAttempt==null || r.LastAttempt<cutoff),NoSuccess=g.Count(r=>r.LastSuccess==null || r.LastSuccess<cutoff)}).SingleOrDefaultAsync(Ct);
        var population=aggregate?.Population ?? 0;var reached=aggregate?.Reached ?? 0;
        var totals=new Dictionary<string,decimal?> { ["roster"]=population,["attempted"]=aggregate?.Attempted ?? 0,["reached"]=reached,["coverage"]=population==0 ? null : Math.Round(100m*reached/population,1),["neverAttempted"]=aggregate?.Never ?? 0,["noRecentAttempt"]=aggregate?.NoAttempt ?? 0,["noRecentSuccess"]=aggregate?.NoSuccess ?? 0 };
        var filteredIds=f.Issue==null ? roster.Select(r=>r.Id) : q.Select(r=>r.Id);
        var callOutcomes=await periodCalls.Where(c=>filteredIds.Contains(c.StudentId)).GroupBy(c=>c.CallStatus).Select(g=>new {Status=g.Key,Count=g.Count()}).ToListAsync(Ct);
        var visitOutcomes=await periodVisits.Where(v=>filteredIds.Contains(v.StudentId)).GroupBy(v=>v.VisitOutcome).Select(g=>new {Status=g.Key,Count=g.Count()}).ToListAsync(Ct);
        foreach(var status in Enum.GetValues<CallStatus>())totals["call"+status]=callOutcomes.FirstOrDefault(o=>o.Status==status)?.Count ?? 0;
        foreach(var status in Enum.GetValues<VisitOutcome>())totals["visit"+status]=visitOutcomes.FirstOrDefault(o=>o.Status==status)?.Count ?? 0;
        return await Table("التواصل والتغطية",f,export,q.OrderBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","المعرف"),new("name","الطالب"),new("owner","المسؤول"),new("attempts","محاولات الفترة","number"),new("reached","تم التواصل بنجاح","boolean"),new("lastAttempt","آخر محاولة","datetime"),new("lastSuccess","آخر تواصل ناجح","datetime")],totals,population);
    }
    private async Task<ReportTable> TaskTable(ReportSuiteFilter f,bool export)
    {
        if(f.Status!=null && f.Status is not ("Open" or "Completed" or "Cancelled" or "PendingReview" or "Due" or "Overdue"))throw new DomainValidationException("Invalid task status.");
        if(f.Type!=null && f.Type is not ("Call" or "Visit"))throw new DomainValidationException("Invalid task type.");
        var ids=StudentFilter(f.StudentFilter,true).Select(s=>s.Id);var (start,end)=Period(f.DateFrom,f.DateTo);
        var q=_context.Set<FollowUpTask>().AsNoTracking().Where(t=>ids.Contains(t.StudentId) && (_caller.IsPrivileged || t.AssigneeId==_caller.UserId));
        if(f.ServantId.HasValue)q=q.Where(t=>t.AssigneeId==f.ServantId);
        if(f.Type!=null)q=q.Where(t=>t.Type==f.Type);
        if(start.HasValue)q=q.Where(t=>t.DueAt>=start);if(end.HasValue)q=q.Where(t=>t.DueAt<end);
        if(f.Status is "Due" or "Overdue")q=q.Where(t=>t.Status=="Open" && t.DueAt<=_caller.UtcNow);
        else if(f.Status!=null)q=q.Where(t=>t.Status==f.Status);
        var totals=new Dictionary<string,decimal?> { ["open"]=await q.CountAsync(t=>t.Status=="Open",Ct),["completed"]=await q.CountAsync(t=>t.Status=="Completed",Ct),["cancelled"]=await q.CountAsync(t=>t.Status=="Cancelled",Ct),["pendingReview"]=await q.CountAsync(t=>t.Status=="PendingReview",Ct),["dueStudents"]=await q.Where(t=>t.Status=="Open" && t.DueAt<=_caller.UtcNow).Select(t=>t.StudentId).Distinct().CountAsync(Ct) };
        var rows=from t in q join s in _context.Students.IgnoreQueryFilters() on t.StudentId equals s.Id
            join u in _context.Users on t.AssigneeId equals (Guid?)u.Id into owners from u in owners.DefaultIfEmpty()
            select new TaskRow {Id=t.Id,StudentId=s.Id,Name=s.FullName,Owner=u==null ? "قائمة إعادة الإسناد" : u.FullName,Type=t.Type,Status=t.Status,DueAt=t.DueAt,CompletedAt=t.CompletedAt,Source=t.SourceType,Reason=t.Reason};
        return await Table("مهام المتابعة",f,export,rows.OrderBy(r=>r.DueAt).ThenBy(r=>r.Id),[new("id","المعرف"),new("studentId","معرف الطالب"),new("name","الطالب"),new("owner","المسؤول"),new("type","النوع"),new("status","الحالة"),new("dueAt","الاستحقاق","datetime"),new("completedAt","الإنجاز","datetime"),new("source","المصدر"),new("reason","السبب")],totals);
    }
    private async Task<ReportTable> WorkloadTable(ReportSuiteFilter f,bool export)
    {
        var roster=SuiteRoster(f);var roleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);var eligibleIds=_context.Users.Where(u=>u.IsActive && _context.UserRoles.Any(r=>r.UserId==u.Id && roleIds.Contains(r.RoleId))).Select(u=>u.Id);var q=roster.Select(s=>new WorkloadRow {Id=s.Id,Name=s.FullName,OwnerId=s.ServantId,Owner=s.Servant==null ? "غير مسند" : s.Servant.FullName,OwnerActive=s.ServantId!=null && eligibleIds.Contains(s.ServantId.Value),Area=s.Area,Year=s.AcademicYear,Tasks=_context.Set<FollowUpTask>().Count(t=>t.StudentId==s.Id && t.Status=="Open")});
        if(f.Issue!=null)q=f.Issue switch {"unassigned"=>q.Where(r=>r.OwnerId==null),"inactive-owner"=>q.Where(r=>r.OwnerId!=null && !r.OwnerActive),_=>throw new DomainValidationException("Invalid workload filter.")};
        var totals=new Dictionary<string,decimal?> { ["roster"]=await q.CountAsync(Ct),["unassigned"]=await q.CountAsync(r=>r.OwnerId==null,Ct),["inactiveOwners"]=await q.CountAsync(r=>r.OwnerId!=null && !r.OwnerActive,Ct),["owners"]=await q.Where(r=>r.OwnerId!=null).Select(r=>r.OwnerId).Distinct().CountAsync(Ct) };
        return await Table("الإسناد وعبء الخدمة",f,export,q.OrderBy(r=>r.Owner).ThenBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","معرف الطالب"),new("name","الطالب"),new("ownerId","معرف المسؤول"),new("owner","المسؤول"),new("ownerActive","مسؤول نشط","boolean"),new("area","المنطقة"),new("year","السنة"),new("tasks","مهام مفتوحة","number")],totals);
    }
    private async Task<ReportTable> OwnerWorkloadTable(ReportSuiteFilter f,bool export)
    {
        var roster=SuiteRoster(f);var (start,end)=Period(f.DateFrom,f.DateTo);var roleIds=_context.Roles.Where(r=>r.Name=="Servant" || r.Name=="Secretary" || r.Name=="Manager").Select(r=>r.Id);var eligibleIds=_context.Users.Where(u=>u.IsActive && _context.UserRoles.Any(r=>r.UserId==u.Id && roleIds.Contains(r.RoleId))).Select(u=>u.Id);
        var attempts=_context.CallLogs.IgnoreQueryFilters().Where(c=>!c.IsVoided && (!start.HasValue || c.CallDate>=start) && (!end.HasValue || c.CallDate<end)).Select(c=>c.StudentId)
            .Union(_context.HomeVisits.IgnoreQueryFilters().Where(v=>!v.IsVoided && (!start.HasValue || v.VisitDate>=start) && (!end.HasValue || v.VisitDate<end)).Select(v=>v.StudentId));
        // Coverage concerns the current filtered cohort and every actor who contacted it.
        var successes=_context.CallLogs.IgnoreQueryFilters().Where(c=>!c.IsVoided && c.CallStatus==CallStatus.Answered && (!start.HasValue || c.CallDate>=start) && (!end.HasValue || c.CallDate<end)).Select(c=>c.StudentId)
            .Union(_context.HomeVisits.IgnoreQueryFilters().Where(v=>!v.IsVoided && v.VisitOutcome==VisitOutcome.Successful && (!start.HasValue || v.VisitDate>=start) && (!end.HasValue || v.VisitDate<end)).Select(v=>v.StudentId));
        var owners=roster.GroupBy(s=>s.ServantId ?? Guid.Empty).Select(g=>new {Id=g.Key,Eligible=g.Count()});
        var attempted=roster.Where(s=>attempts.Contains(s.Id)).GroupBy(s=>s.ServantId ?? Guid.Empty).Select(g=>new {Id=g.Key,Count=g.Count()});
        var reached=roster.Where(s=>successes.Contains(s.Id)).GroupBy(s=>s.ServantId ?? Guid.Empty).Select(g=>new {Id=g.Key,Count=g.Count()});
        var q=from owner in owners join user in _context.Users on owner.Id equals user.Id into users from user in users.DefaultIfEmpty()
            join attempt in attempted on owner.Id equals attempt.Id into attemptsByOwner from attempt in attemptsByOwner.DefaultIfEmpty()
            join success in reached on owner.Id equals success.Id into successesByOwner from success in successesByOwner.DefaultIfEmpty()
            select new OwnerWorkloadRow {Id=owner.Id,Name=user==null ? "غير مسند" : user.FullName,Active=user!=null && eligibleIds.Contains(user.Id),Eligible=owner.Eligible,Attempted=(int?)attempt.Count ?? 0,Present=(int?)success.Count ?? 0};
        if(f.Issue!=null)q=f.Issue switch {"unassigned"=>q.Where(r=>r.Id==Guid.Empty),"inactive-owner"=>q.Where(r=>r.Id!=Guid.Empty && !r.Active),_=>throw new DomainValidationException("Invalid workload filter.")};
        var aggregate=await q.GroupBy(_=>1).Select(g=>new {Owners=g.Count(),Roster=g.Sum(r=>r.Eligible),Attempted=g.Sum(r=>r.Attempted),Reached=g.Sum(r=>r.Present)}).SingleOrDefaultAsync(Ct);
        var totals=new Dictionary<string,decimal?> {["owners"]=aggregate?.Owners ?? 0,["roster"]=aggregate?.Roster ?? 0,["attempted"]=aggregate?.Attempted ?? 0,["reached"]=aggregate?.Reached ?? 0,["coverage"]=aggregate?.Roster>0 ? Math.Round(100m*aggregate.Reached/aggregate.Roster,1) : null};
        return await Table("عبء الخدمة والتغطية لكل مسؤول",f,export,q.OrderBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","معرف المسؤول"),new("name","المسؤول"),new("active","نشط","boolean"),new("eligible","الطلاب المسندون","number"),new("attempted","طلاب جرت محاولة التواصل معهم","number"),new("present","طلاب تم التواصل معهم بنجاح","number"),new("rate","تغطية القائمة %","percent")],totals,aggregate?.Owners ?? 0);
    }
    private async Task<ReportTable> QualityTable(ReportSuiteFilter f,bool export)
    {
        var roster=SuiteRoster(f);
        var duplicateNames=Roster.Where(s=>s.NormalizedFullName!="").GroupBy(s=>s.NormalizedFullName).Where(g=>g.Count()>1).Select(g=>g.Key);
        var phoneRows=Roster.Where(s=>s.NormalizedPhone!=null).Select(s=>new {Id=s.Id,Phone=s.NormalizedPhone!})
            .Concat(Roster.Where(s=>s.NormalizedSecondaryPhone!=null).Select(s=>new {Id=s.Id,Phone=s.NormalizedSecondaryPhone!})).Distinct();
        var duplicatePhones=phoneRows.GroupBy(p=>p.Phone).Where(g=>g.Count()>1).Select(g=>g.Key);
        var today=TimeZoneInfo.ConvertTimeFromUtc(_caller.UtcNow,BusinessTime.Zone).Date;
        var q=(from s in roster join name in duplicateNames on s.NormalizedFullName equals name into names from name in names.DefaultIfEmpty()
            join primary in duplicatePhones on s.NormalizedPhone equals primary into primaries from primary in primaries.DefaultIfEmpty()
            join secondary in duplicatePhones on s.NormalizedSecondaryPhone equals secondary into secondaries from secondary in secondaries.DefaultIfEmpty()
            select new QualityRow {Id=s.Id,Name=s.FullName,Phone=s.Phone,SecondaryPhone=s.SecondaryPhone,MissingPhone=s.NormalizedPhone==null || (s.SecondaryPhone!=null && s.SecondaryPhone!.Trim()!="" && s.NormalizedSecondaryPhone==null),MissingAddress=s.Address==null || s.Address.Trim()=="",MissingArea=s.Area==null || s.Area.Trim()=="",MissingYear=s.AcademicYear==null || s.AcademicYear.Trim()=="",MissingBirthday=s.BirthDate.HasValue ? s.BirthDate>today || s.BirthDate<new DateTime(1900,1,1) : (!s.BirthMonth.HasValue || s.BirthMonth<1 || s.BirthMonth>12),Duplicate=name!=null || primary!=null || secondary!=null}).TagWith("YDM:DataQuality");
        if(f.Issue!=null)q=f.Issue switch {"phone"=>q.Where(r=>r.MissingPhone),"address"=>q.Where(r=>r.MissingAddress),"area"=>q.Where(r=>r.MissingArea),"year"=>q.Where(r=>r.MissingYear),"birthday"=>q.Where(r=>r.MissingBirthday),"duplicates"=>q.Where(r=>r.Duplicate),_=>throw new DomainValidationException("Invalid data quality filter.")};
        else q=q.Where(r=>r.MissingPhone || r.MissingAddress || r.MissingArea || r.MissingYear || r.MissingBirthday || r.Duplicate);
        return await Table("جودة البيانات ومراجعة التشابه",f,export,q.OrderBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","المعرف"),new("name","الطالب"),new("phone","الهاتف","phone"),new("secondaryPhone","الهاتف البديل","phone"),new("missingPhone","هاتف ناقص أو غير صالح","boolean"),new("missingAddress","عنوان ناقص","boolean"),new("missingArea","منطقة ناقصة","boolean"),new("missingYear","سنة ناقصة","boolean"),new("missingBirthday","ميلاد ناقص","boolean"),new("duplicate","تشابه يحتاج مراجعة","boolean")]);
    }
    private IQueryable<ApprovalRow> ApprovalRows(ReportSuiteFilter f)
    {
        var additions=_context.Set<StudentAdditionRequest>().AsNoTracking().Where(r=>_caller.IsPrivileged || r.RequestedByUserId==_caller.UserId);
        var assignments=_context.Set<StudentAssignmentRequest>().IgnoreQueryFilters().AsNoTracking().Where(r=>_caller.IsPrivileged || r.RequestedByUserId==_caller.UserId);
        var removals=_context.Set<StudentRemovalRequest>().IgnoreQueryFilters().AsNoTracking().Where(r=>_caller.IsPrivileged || r.RequestedByUserId==_caller.UserId);
        var matchingStudents=StudentFilter(f.StudentFilter,true).Select(s=>s.Id);
        if(!string.IsNullOrWhiteSpace(f.Search)) {
            var term=YouthDataManager.Domain.Utilities.ArabicNormalizer.Normalize(f.Search.Trim());
            additions=additions.Where(r=>r.FullName.Contains(f.Search.Trim()) || r.Phone.Contains(f.Search.Trim()));
        }
        if(f.Area!=null)additions=f.Area=="__missing__" ? additions.Where(r=>r.Area==null || r.Area.Trim()=="") : additions.Where(r=>r.Area==f.Area);
        if(f.AcademicYear!=null)additions=f.AcademicYear=="__missing__" ? additions.Where(r=>r.AcademicYear==null || r.AcademicYear.Trim()=="") : additions.Where(r=>r.AcademicYear==f.AcademicYear);
        if(f.College!=null)additions=additions.Where(r=>r.College==f.College);
        if(f.Gender.HasValue)additions=additions.Where(r=>(int)r.Gender==f.Gender.Value);
        if(f.BirthMonth.HasValue)additions=additions.Where(r=>(r.BirthDate.HasValue ? r.BirthDate.Value.Month : r.BirthMonth ?? 0)==f.BirthMonth);
        assignments=assignments.Where(r=>matchingStudents.Contains(r.StudentId));removals=removals.Where(r=>matchingStudents.Contains(r.StudentId));
        var (start,end)=Period(f.DateFrom,f.DateTo);
        if(f.ServantId.HasValue){additions=additions.Where(r=>r.RequestedByUserId==f.ServantId);assignments=assignments.Where(r=>r.RequestedByUserId==f.ServantId);removals=removals.Where(r=>r.RequestedByUserId==f.ServantId);}
        if(start.HasValue){additions=additions.Where(r=>r.RequestedAt>=start);assignments=assignments.Where(r=>r.RequestedAt>=start);removals=removals.Where(r=>r.RequestedAt>=start);}
        if(end.HasValue){additions=additions.Where(r=>r.RequestedAt<end);assignments=assignments.Where(r=>r.RequestedAt<end);removals=removals.Where(r=>r.RequestedAt<end);}
        if(f.Status!=null)
        {
            if(!Enum.TryParse<AssignmentRequestStatus>(f.Status,out var status) || !Enum.IsDefined(status))throw new DomainValidationException("Invalid approval status.");
            additions=additions.Where(r=>r.Status==status);assignments=assignments.Where(r=>r.Status==status);var removalStatus=(RemovalRequestStatus)(int)status;removals=removals.Where(r=>r.Status==removalStatus);
        }
        return additions.Select(r=>new ApprovalRow {Id=r.Id,Kind="addition",RequesterId=r.RequestedByUserId,RequesterName=r.RequestedByUser.FullName,ProcessedByName=r.ProcessedByUser==null ? null : r.ProcessedByUser.FullName,RequestedAt=r.RequestedAt,ProcessedAt=r.ProcessedAt,Status=r.Status==AssignmentRequestStatus.Pending ? 0 : r.Status==AssignmentRequestStatus.Approved ? 1 : r.Status==AssignmentRequestStatus.Rejected ? 2 : 3,StudentId=r.CreatedStudentId,Name=r.FullName,Reason=r.ProcessingReason})
            .Concat(assignments.Select(r=>new ApprovalRow {Id=r.Id,Kind="assignment",RequesterId=r.RequestedByUserId,RequesterName=r.RequestedByUser.FullName,ProcessedByName=r.ProcessedByUser==null ? null : r.ProcessedByUser.FullName,RequestedAt=r.RequestedAt,ProcessedAt=r.ProcessedAt,Status=r.Status==AssignmentRequestStatus.Pending ? 0 : r.Status==AssignmentRequestStatus.Approved ? 1 : r.Status==AssignmentRequestStatus.Rejected ? 2 : 3,StudentId=r.StudentId,Name=r.Student.FullName,Reason=r.ProcessingReason}))
            .Concat(removals.Select(r=>new ApprovalRow {Id=r.Id,Kind="removal",RequesterId=r.RequestedByUserId,RequesterName=r.RequestedByUser.FullName,ProcessedByName=r.ProcessedByUser==null ? null : r.ProcessedByUser.FullName,RequestedAt=r.RequestedAt,ProcessedAt=r.ProcessedAt,Status=r.Status==RemovalRequestStatus.Pending ? 0 : r.Status==RemovalRequestStatus.Approved ? 1 : r.Status==RemovalRequestStatus.Rejected ? 2 : 3,StudentId=r.StudentId,Name=r.Student.FullName,Reason=r.ProcessingReason}));
    }
    private async Task<ReportTable> ApprovalTable(ReportSuiteFilter f,bool export)
    {
        if(f.Type!=null && f.Type is not ("addition" or "assignment" or "removal"))throw new DomainValidationException("Invalid request kind.");
        var q=ApprovalRows(f);if(f.Type!=null)q=q.Where(r=>r.Kind==f.Type);
        var totals=new Dictionary<string,decimal?> { ["total"]=await q.CountAsync(Ct),["pending"]=await q.CountAsync(r=>r.Status==0,Ct),["stalePending"]=await q.CountAsync(r=>r.Status==0 && r.RequestedAt<_caller.UtcNow.AddDays(-f.Days),Ct),["averageTurnaroundHours"]=await q.Where(r=>r.ProcessedAt!=null).Select(r=>(decimal?)EF.Functions.DateDiffMinute(r.RequestedAt,r.ProcessedAt)/60).AverageAsync(Ct) };
        return await Table("طلبات الموافقة",f,export,q.OrderByDescending(r=>r.RequestedAt).ThenBy(r=>r.Id),[new("id","معرف الطلب"),new("kind","النوع"),new("requesterName","مقدم الطلب"),new("name","الطالب"),new("processedByName","من عالج الطلب"),new("requestedAt","تاريخ الطلب","datetime"),new("processedAt","تاريخ المعالجة","datetime"),new("status","الحالة","approval-status"),new("studentId","معرف الطالب"),new("reason","سبب الإلغاء / المعالجة")],totals);
    }
    private IQueryable<AttendanceEntry> AttendanceEntries(ReportSuiteFilter f,string kind,bool knownOnly)
    {
        var students=StudentFilter(f.StudentFilter,true).Select(s=>s.Id);
        var q=from r in _context.Set<MeetingRosterEntry>().AsNoTracking() join m in _context.Set<MeetingSession>().AsNoTracking() on r.SessionId equals m.Id
            where r.Kind==kind && m.Status=="Finalized" && (!knownOnly || m.RosterKnown) &&
                (_caller.IsPrivileged || (kind=="Student" ? r.AssignedServantId==_caller.UserId : r.PersonId==_caller.UserId)) && (kind!="Student" || students.Contains(r.PersonId))
            select new AttendanceEntry {Id=r.Id,PersonId=r.PersonId,Name=r.Name,OwnerId=r.AssignedServantId,Date=m.Date,Present=r.Present,RosterKnown=m.RosterKnown};
        if(kind=="Servant" && !string.IsNullOrWhiteSpace(f.Search)){var people=_context.Users.Where(u=>u.FullName.Contains(f.Search.Trim())).Select(u=>u.Id);q=q.Where(r=>people.Contains(r.PersonId));}
        if(f.ServantId.HasValue)q=kind=="Student" ? q.Where(r=>r.OwnerId==f.ServantId) : q.Where(r=>r.PersonId==f.ServantId);
        if(f.DateFrom.HasValue)q=q.Where(r=>r.Date>=f.DateFrom.Value.Date);if(f.DateTo.HasValue)q=q.Where(r=>r.Date<=f.DateTo.Value.Date);
        return q;
    }
    private async Task<ReportTable> AttendanceTable(string report,ReportSuiteFilter f,bool export)
    {
        var kind=report=="attendance" ? "Student" : "Servant";
        // Aggregate IDs, bits and dates before loading bounded person names.
        // Serial plans avoid parallel-worker contention with concurrent report callers.
        var conditions=new List<string>{"r.Kind=@kind","m.Status=N'Finalized'","m.RosterKnown=1"};
        var parameters=new List<SqlParameter>{new("@kind",SqlDbType.NVarChar,20){Value=kind}};
        if(!_caller.IsPrivileged){conditions.Add(kind=="Student" ? "r.AssignedServantId=@caller" : "r.PersonId=@caller");parameters.Add(new("@caller",SqlDbType.UniqueIdentifier){Value=(object?)_caller.UserId ?? DBNull.Value});}
        if(f.ServantId.HasValue){conditions.Add(kind=="Student" ? "r.AssignedServantId=@owner" : "r.PersonId=@owner");parameters.Add(new("@owner",SqlDbType.UniqueIdentifier){Value=f.ServantId.Value});}
        if(f.DateFrom.HasValue){conditions.Add("m.Date>=@from");parameters.Add(new("@from",SqlDbType.Date){Value=f.DateFrom.Value.Date});}
        if(f.DateTo.HasValue){conditions.Add("m.Date<=@to");parameters.Add(new("@to",SqlDbType.Date){Value=f.DateTo.Value.Date});}
        var eligibleSql="SELECT r.PersonId,r.Present,m.Date FROM dbo.MeetingRosterEntry r INNER HASH JOIN dbo.MeetingSession m ON m.Id=r.SessionId WHERE "+string.Join(" AND ",conditions);
        // Only fixed SQL fragments are concatenated; filter values remain typed parameters.
        string AggregateAttendance(string eligible) => "SELECT g.PersonId AS Id,CAST(N'' AS nvarchar(1)) AS Name,g.Eligible,g.Present,g.LastPresent,COUNT(a.PersonId) AS ConsecutiveAbsences FROM (SELECT PersonId,COUNT(*) AS Eligible,SUM(CASE WHEN Present=1 THEN 1 ELSE 0 END) AS Present,MAX(CASE WHEN Present=1 THEN Date END) AS LastPresent FROM ("+eligible+") e GROUP BY PersonId) g LEFT HASH JOIN ("+eligible+") a ON a.PersonId=g.PersonId AND a.Present=0 AND (g.LastPresent IS NULL OR a.Date>g.LastPresent) GROUP BY g.PersonId,g.Eligible,g.Present,g.LastPresent";
        var namesConditions=new List<string>();
        void TextFilter(string field,string parameter,string? value,bool missing=false){if(value==null)return;if(missing && value=="__missing__")namesConditions.Add("(n."+field+" IS NULL OR LTRIM(RTRIM(n."+field+"))=N'')");else {namesConditions.Add("n."+field+"="+parameter);parameters.Add(new(parameter,SqlDbType.NVarChar,4000){Value=value});}}
        if(kind=="Student"){
            _=StudentFilter(f.StudentFilter,true); // identical enum/identifier validation to historical exports
            TextFilter("Area","@area",f.Area,true);TextFilter("AcademicYear","@year",f.AcademicYear,true);TextFilter("College","@college",f.College);
            if(f.Gender.HasValue){namesConditions.Add("n.Gender=@gender");parameters.Add(new("@gender",SqlDbType.Int){Value=f.Gender.Value});}
            if(f.BirthMonth.HasValue){namesConditions.Add("CASE WHEN n.BirthDate IS NOT NULL THEN MONTH(n.BirthDate) ELSE COALESCE(n.BirthMonth,0) END=@month");parameters.Add(new("@month",SqlDbType.Int){Value=f.BirthMonth.Value});}
            if(f.HasServant.HasValue)namesConditions.Add(f.HasServant.Value ? "n.ServantId IS NOT NULL" : "n.ServantId IS NULL");
            if(!string.IsNullOrWhiteSpace(f.Search)){namesConditions.Add("(CHARINDEX(@term,n.NormalizedFullName)>0 OR CHARINDEX(@search,n.Phone)>0)");parameters.Add(new("@term",SqlDbType.NVarChar,4000){Value=YouthDataManager.Domain.Utilities.ArabicNormalizer.Normalize(f.Search.Trim())});parameters.Add(new("@search",SqlDbType.NVarChar,4000){Value=f.Search.Trim()});}
        }else if(!string.IsNullOrWhiteSpace(f.Search)){namesConditions.Add("CHARINDEX(@search,n.FullName)>0");parameters.Add(new("@search",SqlDbType.NVarChar,4000){Value=f.Search.Trim()});}
        // Apply roster filters before both aggregates, avoiding two full-history scans per area/year query.
        if(namesConditions.Count>0)eligibleSql = "SELECT r.PersonId,r.Present,m.Date FROM (SELECT n.Id FROM dbo."+(kind=="Student" ? "Students" : "AspNetUsers")+" n WHERE "+string.Join(" AND ",namesConditions)+") people INNER LOOP JOIN dbo.MeetingRosterEntry r ON r.PersonId=people.Id AND r.Kind=@kind INNER HASH JOIN dbo.MeetingSession m ON m.Id=r.SessionId WHERE "+string.Join(" AND ",conditions);
        var sql=AggregateAttendance(eligibleSql);
        var withTotals="SELECT d.Id,n.FullName AS Name,d.Eligible,d.Present,d.LastPresent,d.ConsecutiveAbsences,SUM(d.Eligible) OVER() AS TotalEligible,SUM(d.Present) OVER() AS TotalPresent,COUNT(*) OVER() AS TotalPeople FROM ("+sql+") d INNER JOIN dbo."+(kind=="Student" ? "Students" : "AspNetUsers")+" n ON n.Id=d.Id"+(namesConditions.Count==0 ? "" : " WHERE "+string.Join(" AND ",namesConditions));
        var q=_context.Database.SqlQueryRaw<AttendanceRow>(withTotals,parameters.ToArray());
        parameters.Add(new("@offset",SqlDbType.Int){Value=export ? 0 : (f.Page-1)*f.PageSize});
        parameters.Add(new("@take",SqlDbType.Int){Value=export ? _exportLimit+1 : f.PageSize});
        var orderedSql=withTotals+" ORDER BY ConsecutiveAbsences DESC,Name,Id OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY OPTION(MAXDOP 1)";
        var rows=await _context.Database.SqlQueryRaw<AttendanceRow>(orderedSql,parameters.ToArray()).ToListAsync(Ct);
        var summary=rows.FirstOrDefault();
        if(summary==null && f.Page>1){parameters.Single(p=>p.ParameterName=="@offset").Value=0;parameters.Single(p=>p.ParameterName=="@take").Value=1;summary=(await _context.Database.SqlQueryRaw<AttendanceRow>(orderedSql,parameters.ToArray()).ToListAsync(Ct)).FirstOrDefault();}
        var eligible=summary?.TotalEligible ?? 0;var present=summary?.TotalPresent ?? 0;
        return await Table(kind=="Student" ? "نسب حضور الطلاب والغياب المتتالي" : "نسب حضور الخدام",f,export,q.OrderByDescending(r=>r.ConsecutiveAbsences).ThenBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","المعرف"),new("name","الاسم"),new("eligible","اجتماعات مؤهلة","number"),new("present","حضور","number"),new("rate","نسبة الحضور","percent"),new("consecutiveAbsences","غياب متتالي","number"),new("lastPresent","آخر حضور","date")],new(){["eligible"]=eligible,["present"]=present,["rate"]=eligible==0 ? null : Math.Round(100m*present/eligible,1)},summary?.TotalPeople ?? 0,rows);
    }
    private async Task<ReportTable> AttendanceHistoryTable(string report,ReportSuiteFilter f,bool export)
    {
        var kind=report=="attendance-history" ? "Student" : "Servant";
        return await Table(kind=="Student" ? "سجل حضور الطلاب" : "سجل حضور الخدام",f,export,AttendanceEntries(f,kind,false).OrderByDescending(r=>r.Date).ThenBy(r=>r.Name).ThenBy(r=>r.Id),[new("id","معرف السجل"),new("personId","معرف الشخص"),new("name","الاسم"),new("date","الاجتماع","date"),new("present","حاضر","boolean"),new("rosterKnown","تغطية القائمة مؤكدة","boolean")]);
    }
    private async Task<ReportTable> MeetingTable(ReportSuiteFilter f,bool export)
    {
        var entries=AttendanceEntries(f,"Student",false);
        var q=_context.Set<MeetingSession>().AsNoTracking();
        if(f.DateFrom.HasValue)q=q.Where(m=>m.Date>=f.DateFrom.Value.Date);if(f.DateTo.HasValue)q=q.Where(m=>m.Date<=f.DateTo.Value.Date);
        var rows=q.Select(m=>new MeetingRow {Id=m.Id,Date=m.Date,Status=m.Status,RosterKnown=m.RosterKnown,Eligible=entries.Count(r=>r.Date==m.Date),Present=entries.Count(r=>r.Date==m.Date && r.Present)});
        return await Table("إجماليات الاجتماعات واتجاهات الحضور",f,export,rows.OrderByDescending(r=>r.Date).ThenBy(r=>r.Id),[new("id","معرف الاجتماع"),new("date","التاريخ","date"),new("status","الحالة"),new("rosterKnown","تغطية مؤكدة","boolean"),new("eligible","القائمة المؤهلة","number"),new("present","الحضور","number"),new("rate","نسبة الحضور","percent")]);
    }
    private sealed class ContactEvent {public Guid StudentId {get;init;}public DateTime At {get;init;}public bool Successful {get;init;}}
    private sealed class ContactRow {public Guid Id {get;init;} public string Name {get;init;}="";public string Owner {get;init;}="";public int Attempts {get;init;}public bool Reached {get;init;}public DateTime? LastAttempt {get;init;}public DateTime? LastSuccess {get;init;} }
    private sealed class TaskRow {public Guid Id {get;init;}public Guid StudentId {get;init;}public string Name {get;init;}="";public string Owner {get;init;}="";public string Type {get;init;}="";public string Status {get;init;}="";public DateTime DueAt {get;init;}public DateTime? CompletedAt {get;init;}public string? Source {get;init;}public string? Reason {get;init;} }
    private sealed class OwnerWorkloadRow {public Guid Id {get;init;}public string Name {get;init;}="";public bool Active {get;init;}public int Eligible {get;init;}public int Attempted {get;init;}public int Present {get;init;}}
    private sealed class WorkloadRow {public Guid Id {get;init;}public string Name {get;init;}="";public Guid? OwnerId {get;init;}public string Owner {get;init;}="";public bool OwnerActive {get;init;}public string Area {get;init;}="";public string Year {get;init;}="";public int Tasks {get;init;} }
    private sealed class QualityRow {public Guid Id {get;init;}public string Name {get;init;}="";public string Phone {get;init;}="";public string? SecondaryPhone {get;init;}public bool MissingPhone {get;init;}public bool MissingAddress {get;init;}public bool MissingArea {get;init;}public bool MissingYear {get;init;}public bool MissingBirthday {get;init;}public bool Duplicate {get;init;} }
    private sealed class ApprovalRow {public Guid Id {get;init;}public string Kind {get;init;}="";public Guid RequesterId {get;init;}public string RequesterName {get;init;}="";public string Name {get;init;}="";public string? ProcessedByName {get;init;}public DateTime RequestedAt {get;init;}public DateTime? ProcessedAt {get;init;}public int Status {get;init;}public Guid? StudentId {get;init;}public string? Reason {get;init;} }
    private sealed class AttendanceEntry {public Guid Id {get;init;}public Guid PersonId {get;init;}public string Name {get;init;}="";public Guid? OwnerId {get;init;}public DateTime Date {get;init;}public bool Present {get;init;}public bool RosterKnown {get;init;} }
    public sealed class AttendanceRow {public Guid Id {get;init;}public string Name {get;init;}="";public int Eligible {get;init;}public int Present {get;init;}public DateTime? LastPresent {get;init;}public int ConsecutiveAbsences {get;init;}public int TotalEligible {get;init;}public int TotalPresent {get;init;}public int TotalPeople {get;init;} }
    private sealed class MeetingRow {public Guid Id {get;init;}public DateTime Date {get;init;}public string Status {get;init;}="";public bool RosterKnown {get;init;}public int Eligible {get;init;}public int Present {get;init;} }
}
