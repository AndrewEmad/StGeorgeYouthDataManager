using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Reports.Service.Implementations.Services;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class ReportTests(SqlFixture fixture)
{
    [Fact]
    public async Task Aggregates_translate_to_SQL_preserve_actors_deduplicate_visit_credit_and_honor_export_filters()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var recorder=await fixture.User(p);var companion=await fixture.User(p);var newOwner=await fixture.User(p);
        var student=new Student {Id=Guid.NewGuid(),FullName="Report",NormalizedFullName="report",Phone="01012345678",Area="ReportTest",ServantId=newOwner.Id,BirthDate=new DateTime(2000,6,3),BirthMonth=1};store.Add(student);await store.Save();
        var when=BusinessTime.UtcStart(new DateTime(2026,4,24)).AddMinutes(30);
        store.Add(new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=recorder.Id,CallDate=when,CallStatus=CallStatus.Answered});
        var visit=new HomeVisit {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=recorder.Id,VisitDate=when,VisitOutcome=VisitOutcome.Successful};
        visit.Participants.Add(new HomeVisitParticipant {HomeVisitId=visit.Id,ServantId=recorder.Id});visit.Participants.Add(new HomeVisitParticipant {HomeVisitId=visit.Id,ServantId=companion.Id});store.Add(visit);await store.Save();
        var provider=new ReportDataProvider(p.GetRequiredService<AppDbContext>(),caller,new ConfigurationBuilder().Build());
        var activity=await provider.Activity(new DateTime(2026,4,24),new DateTime(2026,4,24),recorder.Id,1,25,null,false);
        Assert.Equal(1,activity.Items.Single().CallsInPeriod);Assert.Equal(1,activity.Items.Single().VisitsInPeriod);Assert.Equal(0,activity.Items.Single().AssignedCount);
        Assert.Equal(1,(await provider.Activity(new DateTime(2026,4,24),new DateTime(2026,4,24),companion.Id,1,25,null,false)).Items.Single().VisitsInPeriod);
        Assert.Equal(0,(await provider.Activity(new DateTime(2026,4,24),new DateTime(2026,4,24),newOwner.Id,1,25,null,false)).Items.Single().VisitsInPeriod);
        student.IsDeleted=true;await store.Save();
        Assert.Equal(1,(await provider.Activity(new DateTime(2026,4,24),new DateTime(2026,4,24),companion.Id,1,25,null,false)).Items.Single().VisitsInPeriod);
        student.IsDeleted=false;await store.Save();
        Assert.Equal(3,(await provider.Stats([recorder.Id,companion.Id,newOwner.Id])).Count);
        foreach(var report in new[]{"contact","follow-up","workload","workload-owners","approvals","data-quality","attendance","servant-attendance","attendance-history","servant-attendance-history","meeting-trends"})
        {var table=await provider.Suite(report,new(){Area="ReportTest"});Assert.NotNull(table.Columns);}
        await provider.Performances(1,25,"callsThisWeek",true);await provider.Servants(1,25,null,null,null,"assignedStudentsCount",true);await provider.ManagerDashboard();
        Assert.Contains((await provider.GetStudentsByBirthMonthPagedAsync(1,100)).Items,g=>g.GroupKey=="6" && g.Count>0);
        var calls=await provider.ExportCalls(new(new DateTime(2026,4,24),new DateTime(2026,4,24),recorder.Id,"ReportTest",null));Assert.Single(calls);
        Assert.Empty(await provider.ExportCalls(new(null,null,null,"NoSuchArea",null)));
        student.IsDeleted=true;await store.Save();Assert.Single(await provider.ExportCalls(new(null,null,recorder.Id,"ReportTest",null)));
        using(var studentsBook=new XLWorkbook(new MemoryStream(new ExcelExportService().ExportStudents([student])))){
            var sheet=studentsBook.Worksheet(1);Assert.Equal(XLDataType.DateTime,sheet.Cell(2,10).DataType);Assert.Equal(new DateTime(2000,6,3),sheet.Cell(2,10).GetDateTime());Assert.Equal("yyyy-mm-dd",sheet.Cell(2,10).Style.DateFormat.Format);Assert.Equal(6,sheet.Cell(2,11).GetValue<int>());Assert.Equal(XLDataType.Text,sheet.Cell(2,5).DataType);Assert.Equal("01012345678",sheet.Cell(2,5).GetString());
        }
        var bytes=new ExcelExportService().ExportCalls(calls);using var book=new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal(XLDataType.DateTime,book.Worksheet(1).Cell(2,1).DataType);Assert.Equal("تم الرد",book.Worksheet(1).Cell(2,4).GetString());
    }
    [Fact]
    public async Task Issue_and_status_filters_apply_to_rows_and_full_population_totals()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var owner=await fixture.User(p);
        var area="filters_"+Guid.NewGuid().ToString("N");var reached=new Student{Id=Guid.NewGuid(),FullName="Reached",Phone="01012345678",Area=area,ServantId=owner.Id};var never=new Student{Id=Guid.NewGuid(),FullName="Never",Phone="01012345678",Area=area};store.Add(reached);store.Add(never);await store.Save();
        store.Add(new CallLog{Id=Guid.NewGuid(),StudentId=reached.Id,ServantId=owner.Id,CallDate=DateTime.UtcNow,CallStatus=CallStatus.Answered});store.Add(new FollowUpTask{StudentId=reached.Id,AssigneeId=owner.Id,Status="Completed",DueAt=DateTime.UtcNow});store.Add(new FollowUpTask{StudentId=never.Id,Status="Open",DueAt=DateTime.UtcNow.AddDays(-1)});await store.Save();
        var provider=new ReportDataProvider(p.GetRequiredService<AppDbContext>(),p.GetRequiredService<TestCaller>(),new ConfigurationBuilder().Build());
        var contact=await provider.Suite("contact",new(){Area=area,Issue="never-attempted"});Assert.Equal(1,contact.TotalCount);Assert.Equal(1,contact.Totals["roster"]);Assert.Equal(0,contact.Totals["reached"]);Assert.Equal(0,contact.Totals["callAnswered"]);
        var workload=await provider.Suite("workload",new(){Area=area,Issue="unassigned"});Assert.Equal(1,workload.Totals["roster"]);Assert.Equal(0,workload.Totals["owners"]);
        var tasks=await provider.Suite("follow-up",new(){Area=area,Status="Completed"});Assert.Equal(1,tasks.TotalCount);Assert.Equal(0,tasks.Totals["open"]);Assert.Equal(0,tasks.Totals["dueStudents"]);
        Assert.Equal(contact.TotalCount,(await provider.Suite("contact",new(){Area=area,Issue="never-attempted"},true)).Rows.Count);
    }
    [Fact]
    public void Cairo_calendar_boundaries_handle_the_DST_midnight_gap()
    {
        var start=BusinessTime.UtcStart(new DateTime(2026,4,24));
        var local=TimeZoneInfo.ConvertTimeFromUtc(start,BusinessTime.Zone);
        Assert.Equal(new DateTime(2026,4,24),local.Date);Assert.False(BusinessTime.Zone.IsInvalidTime(local));
        Assert.Equal(TimeSpan.FromHours(23),BusinessTime.UtcStart(new DateTime(2026,4,25))-start);
    }
}
