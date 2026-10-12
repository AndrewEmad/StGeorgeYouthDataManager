using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Infrastructure.Reports;
using YouthDataManager.Reports.Service.Abstractions.DTOs;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class ConcurrentReportTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("attendance")]
    [InlineData("group-attendance")]
    [InlineData("pastoral-follow-up")]
    [InlineData("servant-service-summary")]
    public async Task Concurrent_report_reads_preserve_scope_survive_another_callers_cancellation_and_do_not_cache_completed_results(string report)
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var owner=await fixture.User(scope.ServiceProvider);var area="shared_read_"+Guid.NewGuid();
        var student=new Student {Id=Guid.NewGuid(),FullName="Concurrent report",NormalizedFullName="concurrent report",Phone="01012345678",Area=area,ServantId=owner.Id};var meeting=new MeetingSession {Date=new DateTime(1906,1,5).AddDays(report switch {"group-attendance"=>7,"pastoral-follow-up"=>14,"servant-service-summary"=>21,_=>0}),Status="Finalized",RosterKnown=true};var row=new MeetingRosterEntry {SessionId=meeting.Id,PersonId=student.Id,Name=student.FullName,Present=true,AssignedServantId=owner.Id};db.AddRange(student,meeting,row);await db.SaveChangesAsync();
        var servantRow=new MeetingRosterEntry{SessionId=meeting.Id,PersonId=owner.Id,Name=owner.FullName,Kind="Servant",Present=true};db.Add(servantRow);await db.SaveChangesAsync();
        var gate=new FirstReadGate();var configuration=new ConfigurationBuilder().Build();var filter=new ReportSuiteFilter {Area=report=="servant-service-summary" ? null : area,ServantId=report=="servant-service-summary" ? owner.Id : null,DateFrom=meeting.Date,DateTo=meeting.Date};
        async Task<ReportTable> Read(TestCaller caller){await using var context=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(fixture.Connection).AddInterceptors(gate).Options,caller);return await new ReportDataProvider(context,caller,configuration).Suite(report,filter);}
        decimal? Present(ReportTable table)=>report=="pastoral-follow-up" ? Convert.ToDecimal(table.Rows.Single()["present"]) : table.Totals["present"];
        using var cancellation=new CancellationTokenSource();var original=Read(new(){CancellationToken=cancellation.Token});await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var replacement=Read(new());cancellation.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>original);
        Assert.Equal(1,Present(await replacement));
        if(report=="attendance")Assert.Equal(0,(await Read(new(){IsPrivileged=false,UserId=Guid.NewGuid()})).TotalCount);
        else await Assert.ThrowsAsync<YouthDataManager.Domain.Exceptions.DomainValidationException>(()=>Read(new(){IsPrivileged=false,UserId=Guid.NewGuid()}));
        row.Present=false;servantRow.Present=false;await db.SaveChangesAsync();Assert.Equal(0,Present(await Read(new())));
    }
    private sealed class FirstReadGate:DbCommandInterceptor
    {
        private int blocked;
        public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            if(command.CommandText.Contains("MeetingRosterEntry") && Interlocked.Exchange(ref blocked,1)==0){Entered.TrySetResult();await Task.Delay(Timeout.Infinite,cancellationToken);}
            return result;
        }
    }
}
