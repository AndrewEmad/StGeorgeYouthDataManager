using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class ServiceYearTests(SqlFixture fixture)
{
    [Theory]
    [InlineData(2024,"2024-09-13","2025-09-05")]
    [InlineData(2025,"2025-09-19","2026-09-11")]
    [InlineData(2026,"2026-09-18","2027-09-10")]
    public void Strict_Friday_boundaries(int year,string start,string end)
    {
        var period=ServiceYear.From(year);
        Assert.Equal(start,period.Start.ToString("yyyy-MM-dd"));Assert.Equal(end,period.LastDay.ToString("yyyy-MM-dd"));
        Assert.Equal(DayOfWeek.Friday,period.Start.DayOfWeek);Assert.Equal(DayOfWeek.Friday,period.LastDay.DayOfWeek);
        Assert.Equal(period.LastDay.AddDays(1),TimeZoneInfo.ConvertTimeFromUtc(period.UtcEnd,BusinessTime.Zone));
    }
    [Fact]
    public async Task SQL_scope_preserves_roster_and_filters_events_tasks_requests_and_raw_attendance()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();var caller=p.GetRequiredService<TestCaller>();var store=p.GetRequiredService<IApplicationStore>();
        var actor=await fixture.User(p);caller.UserId=actor.Id;caller.UtcNow=new DateTime(2026,10,13,12,0,0,DateTimeKind.Utc);
        var area=Guid.NewGuid().ToString();var person=new Student {Id=Guid.NewGuid(),FullName="Service year",NormalizedFullName="service year",Phone="01012345678",Area=area,ServantId=actor.Id,CreatedAt=new DateTime(2010,1,1)};
        db.Add(person);var year=ServiceYear.From(2026);var times=new[]{year.UtcStart.AddSeconds(-1),year.UtcStart,year.UtcEnd.AddSeconds(-1),year.UtcEnd};
        foreach(var at in times){db.Add(new CallLog {Id=Guid.NewGuid(),StudentId=person.Id,ServantId=actor.Id,CallDate=at,CallStatus=CallStatus.Answered});db.Add(new FollowUpTask {StudentId=person.Id,AssigneeId=actor.Id,DueAt=at});db.Add(new StudentAdditionRequest {Id=Guid.NewGuid(),RequestedByUserId=actor.Id,RequestedAt=at,FullName="Request",Phone="01012345678"});}
        foreach(var at in new[]{year.UtcStart,year.UtcEnd}){
            var visit=new HomeVisit {Id=Guid.NewGuid(),StudentId=person.Id,ServantId=actor.Id,VisitDate=at,VisitOutcome=VisitOutcome.Successful};
            visit.Participants.Add(new HomeVisitParticipant {HomeVisitId=visit.Id,ServantId=actor.Id});db.Add(visit);
        }
        foreach(var date in new[]{year.Start.AddDays(-7),year.Start,year.LastDay,year.LastDay.AddDays(7)}){
            var meeting=new MeetingSession {Date=date,Status="Finalized",RosterKnown=true};db.Add(meeting);db.Add(new MeetingRosterEntry {SessionId=meeting.Id,PersonId=person.Id,Name=person.FullName,Kind="Student",AssignedServantId=actor.Id,Present=date==year.Start});
        }
        await db.SaveChangesAsync();caller.SelectedServiceYear=year;
        Assert.Equal(1,await store.Query<Student>(false).CountAsync(s=>s.Id==person.Id));
        Assert.Equal(2,await store.Query<CallLog>(false,true).CountAsync(c=>c.StudentId==person.Id));
        Assert.Equal(2,await store.Query<FollowUpTask>(false).CountAsync(t=>t.StudentId==person.Id));
        Assert.Equal(2,await store.Query<StudentAdditionRequest>(false).CountAsync(r=>r.RequestedByUserId==actor.Id));
        var reports=new ReportDataProvider(db,caller,new ConfigurationBuilder().Build());
        var table=await reports.Suite("attendance",new(){Area=area});Assert.Equal(1,table.TotalCount);Assert.Equal(2m,table.Totals["eligible"]);Assert.Equal(1m,table.Totals["present"]);
        var full=await reports.Suite("attendance",new(){Area=area},true);Assert.Equal(table.Totals,full.Totals);Assert.Contains("2026–2027",full.Scope);
        var group=await reports.Suite("group-attendance",new(){Area=area});Assert.Equal(2m,group.Totals["eligible"]);
        Assert.Equal(1,await store.Query<HomeVisit>(false,true).CountAsync(v=>v.StudentId==person.Id));
        var activity=await reports.Activity(null,null,actor.Id,1,25,null,false);Assert.Equal(2,activity.Items.Single().CallsInPeriod);Assert.Equal(1,activity.Items.Single().VisitsInPeriod);
        var narrow=await reports.Suite("attendance",new(){Area=area,DateFrom=year.LastDay,DateTo=year.LastDay});Assert.Equal(1m,narrow.Totals["eligible"]);
        var disjoint=await reports.Suite("attendance",new(){Area=area,DateFrom=new DateTime(2025,10,1),DateTo=new DateTime(2025,10,31)});Assert.Equal(0,disjoint.TotalCount);Assert.Contains("خارج عام الخدمة",disjoint.Period);
        // The same cached EF model must read the request's current scope each time.
        caller.SelectedServiceYear=ServiceYear.From(2025);Assert.Equal(0,await store.Query<CallLog>(false,true).CountAsync(c=>c.StudentId==person.Id));
        person.IsDeleted=true;await db.SaveChangesAsync();caller.SelectedServiceYear=year;
        Assert.Equal(2,await store.Query<CallLog>(false,true).CountAsync(c=>c.StudentId==person.Id));
        caller.SelectedServiceYear=null;caller.AllServiceYears=true;Assert.Equal(4,await store.Query<CallLog>(false,true).CountAsync(c=>c.StudentId==person.Id));
        var allActivity=await reports.Activity(null,null,actor.Id,1,25,null,false);Assert.Equal(4,allActivity.Items.Single().CallsInPeriod);Assert.Equal(2,allActivity.Items.Single().VisitsInPeriod);
        var allAttendance=await reports.Suite("attendance",new(){Area=area});Assert.Equal(4m,allAttendance.Totals["eligible"]);
    }
    [Fact]
    public async Task API_scope_includes_Cairo_end_day_exports_match_and_commands_ignore_read_scope()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();var student=new Student {Id=Guid.NewGuid(),FullName="Year API",NormalizedFullName="year api",Phone="01012345678",ServantId=actor.Id};var year=ServiceYear.From(2026);
        db.Add(student);var outside=new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,CallDate=year.UtcStart.AddDays(-10),CallStatus=CallStatus.Answered};db.Add(outside);
        foreach(var date in new[]{year.UtcStart,year.UtcEnd.AddSeconds(-1),year.UtcEnd})db.Add(new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,CallDate=date,CallStatus=CallStatus.Answered});await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=actor.UserName,password="password123"});login.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        using var result=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/calls?serviceYear=2026"));Assert.Equal(2,result.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/calls?serviceYear=abc")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/calls?serviceYear=2026&serviceYear=2025")).StatusCode);
        using var all=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/calls"));Assert.Equal(4,all.RootElement.GetProperty("totalCount").GetInt32());
        using var explicitAll=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/calls?serviceYear=all"));Assert.Equal(4,explicitAll.RootElement.GetProperty("totalCount").GetInt32());
        var export=await client.GetAsync($"/api/Reports/export/calls?serviceYear=2026&servantId={actor.Id}");export.EnsureSuccessStatusCode();using var bytes=new MemoryStream(await export.Content.ReadAsByteArrayAsync());using var book=new XLWorkbook(bytes);Assert.Equal(3,book.Worksheet(1).LastRowUsed()!.RowNumber());
        var command=await client.PostAsJsonAsync($"/api/FollowUp/call/{outside.Id}/void?serviceYear=2026",new {reason="Year test",version=Convert.ToBase64String(outside.Version)});command.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear();Assert.True((await db.CallLogs.IgnoreQueryFilters().SingleAsync(c=>c.Id==outside.Id)).IsVoided);
    }
}
