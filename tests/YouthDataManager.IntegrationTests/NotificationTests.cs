using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Notifications.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class NotificationTests(SqlFixture fixture)
{
    private sealed class FakePush : IPushTransport
    {
        public List<string> Sent {get;}=[];
        public Task<PushResult> Send(string token,string app,string title,string body,CancellationToken ct){Sent.Add(token);return Task.FromResult(new PushResult(token!="retry",token=="invalid",token=="retry" ? "Unavailable" : null));}
    }
    [Fact]
    public async Task Outbox_commits_with_requests_and_partial_delivery_retries_only_failed_devices()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var db=p.GetRequiredService<AppDbContext>();
        var manager=await fixture.User(p,"Manager");var user=await fixture.User(p);
        var request=new StudentAdditionRequest {Id=Guid.NewGuid(),RequestedByUserId=user.Id,RequestedAt=DateTime.UtcNow,FullName="Notification",Phone="01012345678"};store.Add(request);await store.Save();
        Assert.True(await db.Set<NotificationDelivery>().AnyAsync(d=>d.UserId==manager.Id && d.OccurrenceKey!.Contains(request.Id.ToString())));
        request.Status=AssignmentRequestStatus.Approved;await store.Save();
        var delivery=await db.Set<NotificationDelivery>().SingleAsync(d=>d.UserId==user.Id && d.OccurrenceKey!.Contains(request.Id.ToString()));
        foreach(var token in new[]{"ok","retry"})store.Add(new UserDeviceToken {UserId=user.Id,Token=token,AppType="Servant"});await store.Save();
        var push=new FakePush();var dispatcher=new NotificationDispatcher(db,store,push,new ConfigurationBuilder().Build());var now=DateTime.UtcNow;
        Assert.True(await dispatcher.Deliver(delivery.Id,now,default));Assert.Equal("Pending",delivery.Status);Assert.Equal(2,push.Sent.Count);
        Assert.False(await dispatcher.Deliver(delivery.Id,now,default));
        Assert.True(await dispatcher.Deliver(delivery.Id,now.AddHours(1),default));Assert.Equal(1,push.Sent.Count(t=>t=="ok"));Assert.Equal(2,push.Sent.Count(t=>t=="retry"));
        user.IsActive=false;await store.Save();Assert.True(await dispatcher.Deliver(delivery.Id,now.AddDays(1),default));Assert.Equal("Cancelled",delivery.Status);
    }
    [Fact]
    public async Task Schedule_catches_up_across_midnight_once_and_excludes_pending_review_tasks()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var store=p.GetRequiredService<IApplicationStore>();var db=p.GetRequiredService<AppDbContext>();var user=await fixture.User(p);
        var now=BusinessTime.UtcFromLocal(new DateTime(2026,10,11,0,10,0));var before=now.AddMinutes(-30);
        store.Add(new WorkerCheckpoint {Name="NotificationSchedule",CompletedThrough=before});
        var schedule=new NotificationSchedule {UserId=user.Id,DaysOfWeek="6",TimeOfDay=new TimeOnly(23,50),CreatedAt=before.AddDays(-1)};store.Add(schedule);
        store.Add(new FollowUpTask {StudentId=Guid.NewGuid(),AssigneeId=user.Id,DueAt=before,Status="Open"});await store.Save();
        var worker=new NotificationDispatcher(db,store,new FakePush(),new ConfigurationBuilder().Build());await worker.EnqueueOccurrences(now,default);await worker.EnqueueOccurrences(now,default);
        Assert.Equal(1,await db.Set<NotificationDelivery>().CountAsync(d=>d.OccurrenceKey!.StartsWith("schedule:"+schedule.Id)));
    }
    [Fact]
    public async Task Pausing_a_schedule_cancels_already_queued_reminders_before_provider_delivery()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();var store=p.GetRequiredService<IApplicationStore>();var user=await fixture.User(p);
        var schedule=new NotificationSchedule {UserId=user.Id,DaysOfWeek="5",TimeOfDay=new(18,0),IsActive=false};store.Add(schedule);store.Add(new UserDeviceToken {UserId=user.Id,Token="paused-device",AppType="Servant"});
        var delivery=new NotificationDelivery {UserId=user.Id,OccurrenceKey=$"schedule:{schedule.Id}:20261009:1800",AvailableAt=DateTime.UtcNow.AddMinutes(-1)};store.Add(delivery);await store.Save();
        var push=new FakePush();var worker=new NotificationDispatcher(db,store,push,new ConfigurationBuilder().Build());Assert.True(await worker.Deliver(delivery.Id,DateTime.UtcNow,default));Assert.Equal("Cancelled",delivery.Status);Assert.Empty(push.Sent);
    }
}
