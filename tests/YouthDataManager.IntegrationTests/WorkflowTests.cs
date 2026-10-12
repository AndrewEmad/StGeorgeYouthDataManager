using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Implementations.Commands;
using YouthDataManager.Students.Service.Implementations.Queries;
using YouthDataManager.FollowUp.Service.Implementations.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class WorkflowTests(SqlFixture fixture)
{
    [Fact]
    public async Task Roster_is_snapshotted_marks_are_idempotent_and_correction_reconciles_last_attendance()
    {
        await using var scope=fixture.Provider.CreateAsyncScope(); var p=scope.ServiceProvider; var store=p.GetRequiredService<IApplicationStore>();
        var owner=await fixture.User(p); var s=new Student { Id=Guid.NewGuid(),FullName="حضور",NormalizedFullName="حضور",Phone="01012345678",ServantId=owner.Id };store.Add(s);await store.Save();
        var meetings=new MeetingService(store);var session=(await meetings.Start(new DateTime(2025,12,5))).Data!;
        var row=await store.Query<MeetingRosterEntry>().SingleAsync(r=>r.SessionId==session.Id && r.PersonId==s.Id);Assert.Equal(owner.Id,row.AssignedServantId);
        Assert.Equal(ServiceResultStatus.Success,(await meetings.Mark(session.Id,"Student",[s.Id],true,null)).Status);
        await meetings.Mark(session.Id,"Student",[s.Id],true,null); await meetings.ChangeStatus(session.Id,"Finalized",null);
        Assert.Equal(1,await store.Query<StudentAttendance>().CountAsync(a=>a.StudentId==s.Id && a.Date==session.Date));Assert.Equal(session.Date,s.LastAttendanceDate);
        Assert.Equal(ServiceResultStatus.Failure,(await meetings.Mark(session.Id,"Student",[s.Id],false,null)).Status);
        await meetings.Mark(session.Id,"Student",[s.Id],false,"Incorrect checkbox");Assert.Null((await store.Query<Student>().SingleAsync(x=>x.Id==s.Id)).LastAttendanceDate);
        await meetings.ChangeStatus(session.Id,"Cancelled","Meeting cancelled");Assert.False(await store.Query<StudentAttendance>().AnyAsync(a=>a.StudentId==s.Id));
    }
    [Fact]
    public async Task Task_completion_records_activity_atomically_and_stale_changes_are_rejected()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var owner=await fixture.User(p);caller.UserId=owner.Id;caller.IsPrivileged=false;
        var s=new Student {Id=Guid.NewGuid(),FullName="Task",NormalizedFullName="task",Phone="01012345678",ServantId=owner.Id};store.Add(s);await store.Save();
        var tasks=new FollowUpTaskService(store,caller,new FollowUpCommandsService(store,caller));var task=(await tasks.Create(new(s.Id,"Call",DateTime.UtcNow.AddDays(1)))).Data!;
        var stale=await tasks.Change(task.Id,new("Complete",null,null,null,"stale",(int)CallStatus.Answered,"Reached",DateTime.UtcNow));Assert.Equal(ServiceResultStatus.Failure,stale.Status);
        var completed=await tasks.Change(task.Id,new("Complete",null,null,null,Convert.ToBase64String(task.Version),(int)CallStatus.Answered,"Reached",DateTime.UtcNow));
        Assert.Equal(ServiceResultStatus.Success,completed.Status);task=await store.Query<FollowUpTask>().SingleAsync(t=>t.Id==task.Id);Assert.Equal("Completed",task.Status);
        Assert.True(await store.Query<CallLog>().AnyAsync(c=>c.Id==task.CompletedRecordId));
    }
    [Fact]
    public async Task Discovery_returns_only_minimal_student_fields_and_assigned_queries_translate_in_SQL()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var owner=await fixture.User(p);caller.UserId=owner.Id;caller.IsPrivileged=false;
        var s=new Student {Id=Guid.NewGuid(),FullName="Private",NormalizedFullName="private",Phone="01012345678",Address="Private address",Notes="Private note",ServantId=null};store.Add(s);await store.Save();
        var queries=new StudentQueriesService(store,caller);var discovery=(await queries.GetUnassignedForServant(owner.Id,1,100)).Data!;
        Assert.IsType<StudentDiscoveryDto>(discovery.Items.Single(i=>((StudentDiscoveryDto)i.Student).Id==s.Id).Student);
        Assert.Null((await queries.GetById(s.Id)).Data);
        s.ServantId=owner.Id;await store.Save();Assert.Equal("Private address",(await queries.GetById(s.Id)).Data!.Address);
        Assert.Single((await queries.GetPaged(new(null,null,null,null,null,null,null,null),1,25)).Data!.Items);
    }
}
