using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class AttendanceReportTests(SqlFixture fixture)
{
    [Fact]
    public async Task Filtered_totals_pagination_absence_streaks_and_historical_ownership_agree()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;
        var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var original=await fixture.User(p);var current=await fixture.User(p);var area="attendance_"+Guid.NewGuid().ToString("N");
        var a=new Student {Id=Guid.NewGuid(),FullName="Arabic % name",NormalizedFullName="arabic % name",Phone="01012345678",Area=area,ServantId=current.Id,IsDeleted=true};
        var b=new Student {Id=Guid.NewGuid(),FullName="Never present",NormalizedFullName="never present",Phone="01012345679",Area=area,ServantId=current.Id};
        store.Add(a);store.Add(b);await store.Save();
        for(var i=0;i<5;i++){
            var meeting=new MeetingSession {Date=new DateTime(1905,1,6).AddDays(7*i),Status=i==3 ? "Cancelled" : "Finalized",RosterKnown=i!=4};store.Add(meeting);
            foreach(var student in new[]{a,b})store.Add(new MeetingRosterEntry {SessionId=meeting.Id,PersonId=student.Id,Name=student.FullName,AssignedServantId=original.Id,Present=student==a && (i==0 || i==4)});
        }
        await store.Save();var report=new ReportDataProvider(p.GetRequiredService<AppDbContext>(),caller,new ConfigurationBuilder().Build());
        var first=await report.Suite("attendance",new(){Area=area,PageSize=1});
        Assert.Equal(2,first.TotalCount);Assert.Single(first.Rows);Assert.Equal(6,first.Totals["eligible"]);Assert.Equal(1,first.Totals["present"]);
        Assert.Equal(3,first.Rows[0]["consecutiveAbsences"]);
        var second=await report.Suite("attendance",new(){Area=area,Page=2,PageSize=1});Assert.Equal(2,second.Rows[0]["consecutiveAbsences"]);Assert.Equal(first.Totals,second.Totals);
        var beyond=await report.Suite("attendance",new(){Area=area,Page=4,PageSize=1});Assert.Empty(beyond.Rows);Assert.Equal(2,beyond.TotalCount);
        var export=await report.Suite("attendance",new(){Area=area},true);Assert.Equal(2,export.Rows.Count);Assert.Equal(first.Totals,export.Totals);
        var empty=await report.Suite("attendance",new(){Area=area,Search="does not exist"});Assert.Equal(0,empty.TotalCount);Assert.Null(empty.Totals["rate"]);
        var literal=await report.Suite("attendance",new(){Area=area,Search="%"});Assert.Equal(1,literal.TotalCount);Assert.Equal(3,literal.Totals["eligible"]);
        caller.IsPrivileged=false;caller.UserId=original.Id;
        Assert.Equal(2,(await report.Suite("attendance",new(){Area=area})).TotalCount);
        caller.UserId=current.Id;Assert.Equal(0,(await report.Suite("attendance",new(){Area=area})).TotalCount);
    }
}
