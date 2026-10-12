using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Exceptions;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class PastoralReportTests(SqlFixture fixture)
{
    private static ReportDataProvider Provider(IServiceProvider p) => new(p.GetRequiredService<AppDbContext>(),p.GetRequiredService<TestCaller>(),new ConfigurationBuilder().Build());
    private static Student Student(string area,Guid? owner,string name) => new(){Id=Guid.NewGuid(),FullName=name,NormalizedFullName=name.ToLowerInvariant(),Phone="01012345678",Area=area,ServantId=owner};

    [Fact]
    public async Task Service_summary_credits_recorded_actors_once_keeps_inactive_history_and_zero_activity_people()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();
        var recorder=await fixture.User(p);var companion=await fixture.User(p);var currentOwner=await fixture.User(p);
        var name="summary"+Guid.NewGuid().ToString("N");recorder.FullName=companion.FullName=currentOwner.FullName=name;recorder.NormalizedFullName=companion.NormalizedFullName=currentOwner.NormalizedFullName=name;
        var a=Student(name,currentOwner.Id,"Transferred");var b=Student(name,recorder.Id,"Archived");b.IsDeleted=true;db.AddRange(a,b);
        var date=new DateTime(1997,5,9);var when=BusinessTime.UtcStart(date).AddHours(20);
        db.AddRange(new CallLog{Id=Guid.NewGuid(),StudentId=a.Id,ServantId=recorder.Id,CallDate=when,CallStatus=CallStatus.Answered},new CallLog{Id=Guid.NewGuid(),StudentId=b.Id,ServantId=recorder.Id,CallDate=when,CallStatus=CallStatus.NotAnswered},new CallLog{Id=Guid.NewGuid(),StudentId=a.Id,ServantId=recorder.Id,CallDate=when,CallStatus=CallStatus.Answered,IsVoided=true},new CallLog{Id=Guid.NewGuid(),StudentId=a.Id,ServantId=recorder.Id,CallDate=BusinessTime.UtcStart(date.AddDays(1)),CallStatus=CallStatus.Answered});
        var visit=new HomeVisit{Id=Guid.NewGuid(),StudentId=a.Id,ServantId=recorder.Id,VisitDate=when,VisitOutcome=VisitOutcome.Successful};
        visit.Participants.Add(new(){HomeVisitId=visit.Id,ServantId=recorder.Id});visit.Participants.Add(new(){HomeVisitId=visit.Id,ServantId=companion.Id});db.Add(visit);
        db.Add(new HomeVisit{Id=Guid.NewGuid(),StudentId=b.Id,ServantId=recorder.Id,VisitDate=when,VisitOutcome=VisitOutcome.NotAtHome});
        var meeting=new MeetingSession{Date=date,Status="Finalized"};db.Add(meeting);db.Add(new MeetingRosterEntry{SessionId=meeting.Id,Kind="Servant",PersonId=recorder.Id,Name=name,Present=true});
        var now=p.GetRequiredService<TestCaller>().UtcNow;db.AddRange(new FollowUpTask{StudentId=a.Id,AssigneeId=recorder.Id,Status="Open",DueAt=now.AddDays(-1)},new FollowUpTask{StudentId=a.Id,AssigneeId=recorder.Id,Status="PendingReview",DueAt=now.AddDays(-1)});
        companion.IsActive=false;await db.SaveChangesAsync();Assert.True((await p.GetRequiredService<UserManager<ApplicationUser>>().RemoveFromRoleAsync(companion,"Servant")).Succeeded);
        var provider=Provider(p);var filter=new ReportSuiteFilter{DateFrom=date,DateTo=date,Search=name,PageSize=1};
        var first=await provider.Suite("servant-service-summary",filter);Assert.Equal(3,first.TotalCount);Assert.Single(first.Rows);Assert.Equal(2,first.Totals["calls"]);Assert.Equal(2,first.Totals["visits"]);Assert.Equal(3,first.Totals["visitCredits"]);Assert.Equal(100,first.Totals["rate"]);
        filter.Page=2;var second=await provider.Suite("servant-service-summary",filter);Assert.Equal(first.Totals,second.Totals);
        var full=await provider.Suite("servant-service-summary",filter,true);Assert.Equal(3,full.Rows.Count);Assert.Equal(first.Totals,full.Totals);
        var recorded=full.Rows.Single(r=>(Guid)r["id"]! ==recorder.Id);Assert.Equal(2,recorded["calls"]);Assert.Equal(1,recorded["callAnswered"]);Assert.Equal(1,recorded["callNotAnswered"]);Assert.Equal(2,recorded["visits"]);Assert.Equal(2,recorded["attempted"]);Assert.Equal(1,recorded["reached"]);Assert.Equal(0,recorded["assignedStudentsCount"]);Assert.Equal(1,recorded["openTasks"]);Assert.Equal(1,recorded["overdueTasks"]);
        var inactive=full.Rows.Single(r=>(Guid)r["id"]! ==companion.Id);Assert.Equal(false,inactive["active"]);Assert.Equal(1,inactive["visits"]);
        var owner=full.Rows.Single(r=>(Guid)r["id"]! ==currentOwner.Id);Assert.Equal(1,owner["assignedStudentsCount"]);Assert.Equal(0,owner["calls"]);Assert.Equal(0,owner["visits"]);Assert.Null(owner["rate"]);
        var none=await provider.Suite("servant-service-summary",new(){DateFrom=date,DateTo=date,Search="notfound"+name});Assert.Empty(none.Rows);Assert.Null(none.Totals["rate"]);
    }

    [Fact]
    public async Task Historical_groups_and_meeting_totals_use_confirmed_rosters_and_snapshot_ownership()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();var oldOwner=await fixture.User(p);var currentOwner=await fixture.User(p);
        var area="groups"+Guid.NewGuid().ToString("N");var a=Student(area,currentOwner.Id,"Moved");var b=Student(area,null,"Unassigned");a.IsDeleted=true;db.AddRange(a,b);var date=new DateTime(1998,1,2);
        for(var i=0;i<4;i++){
            var meeting=new MeetingSession{Date=date.AddDays(7*i),Status=i==2 ? "Cancelled" : "Finalized",RosterKnown=i!=3};db.Add(meeting);
            db.AddRange(new MeetingRosterEntry{SessionId=meeting.Id,PersonId=a.Id,Name=a.FullName,AssignedServantId=oldOwner.Id,Present=i==0 || i==3},new MeetingRosterEntry{SessionId=meeting.Id,PersonId=b.Id,Name=b.FullName,Present=i==0},new MeetingRosterEntry{SessionId=meeting.Id,Kind="Servant",PersonId=oldOwner.Id,Name=oldOwner.FullName,Present=i==0});
        }
        await db.SaveChangesAsync();var provider=Provider(p);var filter=new ReportSuiteFilter{Area=area,DateFrom=date,DateTo=date.AddDays(21),PageSize=1};
        var groups=await provider.Suite("group-attendance",filter);Assert.Equal(2,groups.TotalCount);Assert.Equal(4,groups.Totals["eligible"]);Assert.Equal(2,groups.Totals["present"]);Assert.Equal(2,groups.Totals["absent"]);Assert.Equal(50,groups.Totals["rate"]);
        var full=await provider.Suite("group-attendance",filter,true);Assert.Contains(full.Rows,r=>(Guid)r["id"]! ==oldOwner.Id);Assert.DoesNotContain(full.Rows,r=>(Guid)r["id"]! ==currentOwner.Id);
        var drilldown=await provider.Suite("attendance",new(){Area=area,DateFrom=date,DateTo=date.AddDays(21),ServantId=oldOwner.Id});Assert.Single(drilldown.Rows);Assert.Equal(2,drilldown.Totals["eligible"]);
        var unassigned=await provider.Suite("attendance",new(){Area=area,DateFrom=date,DateTo=date.AddDays(21),Issue="unassigned-group"});Assert.Single(unassigned.Rows);Assert.Equal(b.Id,unassigned.Rows[0]["id"]);
        var meetings=await provider.Suite("meeting-trends",filter,true);Assert.Equal(4,meetings.Rows.Count);Assert.Equal(4,meetings.Totals["eligible"]);Assert.Equal(2,meetings.Totals["servantEligible"]);Assert.Equal(1,meetings.Totals["servantPresent"]);Assert.Equal(50,meetings.Totals["servantRate"]);
        Assert.All(meetings.Rows.Where(r=>(string)r["status"]! =="Cancelled" || !(bool)r["rosterKnown"]!),r=>{Assert.Null(r["rate"]);Assert.Null(r["servantRate"]);});
    }

    [Fact]
    public async Task Pastoral_queue_explains_absences_unknown_coverage_recent_success_and_current_tasks_without_creating_work()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();var oldOwner=await fixture.User(p);var owner=await fixture.User(p);
        var date=new DateTime(2004,1,2);var last=date.AddDays(21);var when=BusinessTime.UtcStart(last).AddHours(23);p.GetRequiredService<TestCaller>().UtcNow=when;
        var area="pastoral"+Guid.NewGuid().ToString("N");var absent=Student(area,owner.Id,"Absent");var unknown=Student(area,owner.Id,"Unknown");var never=Student(area,null,"Never");var healthy=Student(area,owner.Id,"Healthy");var overdue=Student(area,owner.Id,"Overdue");db.AddRange(absent,unknown,never,healthy,overdue);
        for(var i=0;i<6;i++){
            var meeting=new MeetingSession{Date=date.AddDays(i<4 ? i*7 : i==4 ? 3 : 4),Status=i==4 ? "Cancelled" : "Finalized",RosterKnown=i!=5};db.Add(meeting);
            if(i==5)db.Add(new MeetingRosterEntry{SessionId=meeting.Id,PersonId=unknown.Id,Name=unknown.FullName,Present=true});
            foreach(var student in new[]{absent,healthy,overdue})db.Add(new MeetingRosterEntry{SessionId=meeting.Id,PersonId=student.Id,Name=student.FullName,AssignedServantId=oldOwner.Id,Present=student!=absent || i==0 || i==5});
        }
        foreach(var student in new[]{absent,unknown,healthy,overdue})db.Add(new CallLog{Id=Guid.NewGuid(),StudentId=student.Id,ServantId=oldOwner.Id,CallDate=when,CallStatus=CallStatus.Answered});
        db.AddRange(new FollowUpTask{StudentId=overdue.Id,AssigneeId=owner.Id,DueAt=when.AddHours(-1)},new FollowUpTask{StudentId=healthy.Id,AssigneeId=owner.Id,DueAt=when.AddHours(-1),Status="PendingReview"});await db.SaveChangesAsync();var taskCount=await db.Set<FollowUpTask>().CountAsync();
        var provider=Provider(p);var filter=new ReportSuiteFilter{Area=area,DateFrom=date,DateTo=last,PageSize=1};var first=await provider.Suite("pastoral-follow-up",filter);Assert.Equal(4,first.TotalCount);Assert.Equal(1,first.Totals["consecutiveAbsenceStudents"]);Assert.Equal(2,first.Totals["unknownAttendanceStudents"]);Assert.Equal(1,first.Totals["neverAttempted"]);Assert.Equal(1,first.Totals["overdueStudents"]);
        var full=await provider.Suite("pastoral-follow-up",filter,true);Assert.Equal(first.Totals,full.Totals);Assert.Equal(4,full.Rows.Count);Assert.DoesNotContain(full.Rows,r=>(Guid)r["id"]! ==healthy.Id);
        var a=full.Rows.Single(r=>(Guid)r["id"]! ==absent.Id);Assert.Equal(3,a["consecutiveAbsences"]);Assert.Equal(owner.Id,a["ownerId"]);Assert.Equal(date,a["lastPresent"]);Assert.Equal("غياب متتالي",a["reason"]);
        var u=full.Rows.Single(r=>(Guid)r["id"]! ==unknown.Id);Assert.Equal(0,u["consecutiveAbsences"]);Assert.Null(u["lastPresent"]);Assert.Null(u["rate"]);Assert.Equal("بيانات حضور غير كافية في الفترة",u["reason"]);
        filter.Issue="overdue-tasks";Assert.Single((await provider.Suite("pastoral-follow-up",filter)).Rows);filter.Issue="never-attempted";Assert.Single((await provider.Suite("pastoral-follow-up",filter)).Rows);
        filter.MinConsecutiveAbsences=0;await Assert.ThrowsAsync<DomainValidationException>(()=>provider.Suite("pastoral-follow-up",filter));Assert.Equal(taskCount,await db.Set<FollowUpTask>().CountAsync());
    }

    [Fact]
    public async Task New_report_API_requires_privilege_and_exports_the_same_filtered_rows_and_native_dates()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var servant=await fixture.User(p);var manager=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();
        var area="export"+Guid.NewGuid().ToString("N");var student=Student(area,servant.Id,"Exported");db.Add(student);var date=new DateTime(1996,3,1);var meeting=new MeetingSession{Date=date,Status="Finalized"};db.Add(meeting);db.Add(new MeetingRosterEntry{SessionId=meeting.Id,PersonId=student.Id,Name=student.FullName,AssignedServantId=servant.Id,Present=true});await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        async Task Login(ApplicationUser user){var response=await client.PostAsJsonAsync("/api/Auth/login",new{username=user.UserName,password="password123"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);var login=(await response.Content.ReadFromJsonAsync<LoginResponse>())!;client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);}
        await Login(servant);
        foreach(var report in new[]{"servant-service-summary","group-attendance","pastoral-follow-up"})foreach(var suffix in new[]{"","?full=true","/export?format=csv","/export?format=xlsx"})Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/reports/suite/"+report+suffix)).StatusCode);
        await Login(manager);
        foreach(var report in new[]{"servant-service-summary","group-attendance","pastoral-follow-up"}){
            var filters=$"dateFrom=1996-03-01&dateTo=1996-03-01&servantId={servant.Id}"+(report=="servant-service-summary" ? "" : "&area="+area);
            var response=await client.GetAsync($"/api/reports/suite/{report}?{filters}");Assert.Equal(HttpStatusCode.OK,response.StatusCode);var table=(await response.Content.ReadFromJsonAsync<ReportTable>())!;Assert.Equal(1,table.TotalCount);
            var all=(await client.GetFromJsonAsync<ReportTable>($"/api/reports/suite/{report}?{filters}&full=true"))!;Assert.Equal(table.TotalCount,all.Rows.Count);
            var xlsx=await client.GetAsync($"/api/reports/suite/{report}/export?{filters}");Assert.Equal(HttpStatusCode.OK,xlsx.StatusCode);using var book=new XLWorkbook(new MemoryStream(await xlsx.Content.ReadAsByteArrayAsync()));Assert.Equal(table.TotalCount+1,book.Worksheet(1).LastRowUsed()!.RowNumber());
            if(report=="pastoral-follow-up"){
                var phoneColumn=table.Columns.ToList().FindIndex(c=>c.Key=="phone")+1;Assert.Equal(XLDataType.Text,book.Worksheet(1).Cell(2,phoneColumn).DataType);Assert.Equal("01012345678",book.Worksheet(1).Cell(2,phoneColumn).GetString());
                var dateColumn=table.Columns.ToList().FindIndex(c=>c.Key=="lastPresent")+1;Assert.Equal(XLDataType.DateTime,book.Worksheet(1).Cell(2,dateColumn).DataType);
            }
            var csv=await client.GetAsync($"/api/reports/suite/{report}/export?{filters}&format=csv");Assert.Equal(HttpStatusCode.OK,csv.StatusCode);var text=await csv.Content.ReadAsStringAsync();Assert.Contains(table.Columns[0].Label,text);Assert.DoesNotContain("servant-service-summary",text);
        }
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/reports/suite/pastoral-follow-up?minConsecutiveAbsences=366")).StatusCode);
    }
}
