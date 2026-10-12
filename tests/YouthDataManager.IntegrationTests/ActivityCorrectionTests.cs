using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.FollowUp.Service.Implementations.Commands;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class ActivityCorrectionTests(SqlFixture fixture)
{
    [Fact]
    public async Task Corrections_preserve_audit_reject_stale_versions_and_void_cancels_open_work()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;
        var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var owner=await fixture.User(p);caller.UserId=owner.Id;caller.IsPrivileged=false;
        var s=new Student {Id=Guid.NewGuid(),FullName="Correction",NormalizedFullName="correction",Phone="01012345678",ServantId=owner.Id};store.Add(s);await store.Save();
        var service=new FollowUpCommandsService(store,caller);var date=DateTime.UtcNow.AddHours(-1);
        var id=(await service.RegisterCall(new(s.Id,date,CallStatus.NotAnswered,"Original",date.AddDays(1)),owner.Id)).Data;
        var call=await store.Query<CallLog>().SingleAsync(c=>c.Id==id);var version=Convert.ToBase64String(call.Version);
        Assert.Equal(ServiceResultStatus.Success,(await service.UpdateCall(id,new(date,CallStatus.Answered,"Corrected",null,"Confirmed outcome",version),owner.Id)).Status);
        Assert.Equal("Cancelled",(await store.Query<FollowUpTask>().SingleAsync(t=>t.SourceId==id)).Status);
        Assert.True(await store.Query<AuditEntry>().AnyAsync(a=>a.EntityId==id.ToString() && a.Reason=="Confirmed outcome" && a.BeforeJson.Contains("Original") && a.AfterJson.Contains("Corrected")));
        Assert.Contains((await service.UpdateCall(id,new(date,CallStatus.Busy,"Stale",null,"Another edit",version),owner.Id)).Errors,e=>e.Code=="Conflict");
        call=await store.Query<CallLog>().SingleAsync(c=>c.Id==id);Assert.Equal(CallStatus.Answered,call.CallStatus);
        var current=Convert.ToBase64String(call.Version);
        Assert.Equal(ServiceResultStatus.Success,(await service.Void(id,false,new("Duplicate record",current))).Status);
        call=await store.Query<CallLog>().SingleAsync(c=>c.Id==id);Assert.True(call.IsVoided);Assert.Equal("Corrected",call.Notes);
        Assert.Equal(ServiceResultStatus.Success,(await service.Void(id,false,new("Duplicate record",current))).Status);
    }
    [Fact]
    public async Task Only_current_assigned_recorder_or_privileged_actor_can_correct_historical_activity()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var caller=p.GetRequiredService<TestCaller>();
        var owner=await fixture.User(p);var other=await fixture.User(p);caller.UserId=owner.Id;caller.IsPrivileged=false;
        var s=new Student {Id=Guid.NewGuid(),FullName="Archived",NormalizedFullName="archived",Phone="01012345678",ServantId=owner.Id};store.Add(s);await store.Save();
        var service=new FollowUpCommandsService(store,caller);var date=DateTime.UtcNow.AddDays(-1);var id=(await service.RegisterVisit(new(s.Id,date,VisitOutcome.Successful,"Original",null,[other.Id]),owner.Id)).Data;
        var visit=await store.Query<HomeVisit>().SingleAsync(v=>v.Id==id);var version=Convert.ToBase64String(visit.Version);
        s.ServantId=other.Id;await store.Save();
        Assert.Contains((await service.UpdateVisit(id,new(date,VisitOutcome.Postponed,"No access",null,[other.Id],"Correction",version),owner.Id)).Errors,e=>e.Code=="Forbidden");
        s=await store.Query<Student>().SingleAsync(x=>x.Id==s.Id);s.IsDeleted=true;await store.Save();caller.IsPrivileged=true;
        Assert.Equal(ServiceResultStatus.Success,(await service.UpdateVisit(id,new(date,VisitOutcome.Postponed,"Historical correction",null,[other.Id],"Correction",version),owner.Id)).Status);
    }
}
