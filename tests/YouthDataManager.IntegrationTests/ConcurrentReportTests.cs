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
    [Fact]
    public async Task Concurrent_attendance_reads_preserve_scope_survive_another_callers_cancellation_and_do_not_cache_completed_results()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();var owner=await fixture.User(scope.ServiceProvider);var area="shared_read_"+Guid.NewGuid();
        var student=new Student {Id=Guid.NewGuid(),FullName="Concurrent report",NormalizedFullName="concurrent report",Phone="01012345678",Area=area,ServantId=owner.Id};var meeting=new MeetingSession {Date=new(1906,1,5),Status="Finalized",RosterKnown=true};var row=new MeetingRosterEntry {SessionId=meeting.Id,PersonId=student.Id,Name=student.FullName,Present=true,AssignedServantId=owner.Id};db.AddRange(student,meeting,row);await db.SaveChangesAsync();
        var gate=new FirstReadGate();var configuration=new ConfigurationBuilder().Build();var filter=new ReportSuiteFilter {Area=area};
        async Task<ReportTable> Read(TestCaller caller){await using var context=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(fixture.Connection).AddInterceptors(gate).Options,caller);return await new ReportDataProvider(context,caller,configuration).Suite("attendance",filter);}
        using var cancellation=new CancellationTokenSource();var original=Read(new(){CancellationToken=cancellation.Token});await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var replacement=Read(new());cancellation.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>original);
        Assert.Equal(1,(await replacement).Totals["present"]);
        Assert.Equal(0,(await Read(new(){IsPrivileged=false,UserId=Guid.NewGuid()})).TotalCount);
        row.Present=false;await db.SaveChangesAsync();Assert.Equal(0,(await Read(new())).Totals["present"]);
    }
    private sealed class FirstReadGate:DbCommandInterceptor
    {
        private int blocked;
        public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken cancellationToken=default)
        {
            if(command.CommandText.Contains("ConsecutiveAbsences") && Interlocked.Exchange(ref blocked,1)==0){Entered.TrySetResult();await Task.Delay(Timeout.Infinite,cancellationToken);}
            return result;
        }
    }
}
