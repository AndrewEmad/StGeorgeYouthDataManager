using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Notifications.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Infrastructure.Data;

public sealed class NotificationDispatcher(AppDbContext db,IApplicationStore store,IPushTransport transport,IConfiguration config)
{
    public async Task EnqueueOccurrences(DateTime now,CancellationToken ct)
    {
        await store.Transaction(async()=>{
            var checkpoint=await db.Set<WorkerCheckpoint>().SingleOrDefaultAsync(w=>w.Name=="NotificationSchedule",ct);
            if(checkpoint==null){checkpoint=new(){Name="NotificationSchedule",CompletedThrough=now.AddDays(-Math.Clamp(int.TryParse(config["NotificationWorker:InitialCatchUpDays"],out var days) ? days : 7,1,30))};db.Add(checkpoint);}
            var from=checkpoint.CompletedThrough;var through=now<from.AddDays(7) ? now : from.AddDays(7);
            var localStart=TimeZoneInfo.ConvertTimeFromUtc(from,BusinessTime.Zone).Date;var localEnd=TimeZoneInfo.ConvertTimeFromUtc(through,BusinessTime.Zone).Date;
            var schedules=await db.NotificationSchedules.AsNoTracking().Where(s=>s.IsActive && s.User.IsActive).ToListAsync(ct);
            foreach(var schedule in schedules)
            {
                var days=schedule.DaysOfWeek.Split(',').Select(s=>int.TryParse(s,out var day) ? day : -1).ToHashSet();
                for(var date=localStart;date<=localEnd;date=date.AddDays(1))
                {
                    ct.ThrowIfCancellationRequested();if(!days.Contains((int)date.DayOfWeek))continue;
                    var occurrence=BusinessTime.UtcFromLocal(date.Add(schedule.TimeOfDay.ToTimeSpan()));
                    if(occurrence<=from || occurrence>through || occurrence<schedule.CreatedAt || occurrence<(schedule.UpdatedAt ?? schedule.CreatedAt))continue;
                    var key=$"schedule:{schedule.Id}:{date:yyyyMMdd}:{schedule.TimeOfDay:HHmm}";
                    if(await db.Set<NotificationDelivery>().AnyAsync(d=>d.OccurrenceKey==key,ct))continue;
                    var count=await db.Set<FollowUpTask>().Where(t=>t.AssigneeId==schedule.UserId && t.Status=="Open" && t.DueAt<=occurrence).Select(t=>t.StudentId).Distinct().CountAsync(ct);
                    if(count==0)continue;
                    db.Add(new NotificationDelivery {UserId=schedule.UserId,AppType="Any",OccurrenceKey=key,AvailableAt=now,Title="تذكير متابعة",Body=$"لديك {count} مخدومًا لهم مهام مستحقة."});
                }
            }
            checkpoint.CompletedThrough=through;await store.Save(ct);return true;
        });
    }
    public Task<Guid[]> Due(DateTime now,CancellationToken ct) => db.Set<NotificationDelivery>().AsNoTracking()
        .Where(d=>d.Status=="Pending" && d.AvailableAt<=now && (d.ClaimedUntil==null || d.ClaimedUntil<now))
        .OrderBy(d=>d.AvailableAt).ThenBy(d=>d.Id).Take(50).Select(d=>d.Id).ToArrayAsync(ct);
    public async Task<bool> Deliver(Guid id,DateTime now,CancellationToken ct)
    {
        var claim=Guid.NewGuid();
        var acquired=await db.Set<NotificationDelivery>().Where(d=>d.Id==id && d.Status=="Pending" && d.AvailableAt<=now && (d.ClaimedUntil==null || d.ClaimedUntil<now))
            .ExecuteUpdateAsync(u=>u.SetProperty(d=>d.ClaimId,claim).SetProperty(d=>d.ClaimedUntil,now.AddMinutes(5)),ct);
        if(acquired!=1)return false;
        var delivery=await db.Set<NotificationDelivery>().SingleAsync(d=>d.Id==id && d.ClaimId==claim,ct);
        await db.Entry(delivery).ReloadAsync(ct);
        var active=await db.Users.AnyAsync(u=>u.Id==delivery.UserId && u.IsActive,ct);
        if(!active){delivery.Status="Cancelled";delivery.LastError="Account inactive";await Finish(delivery,ct);return true;}
        if(delivery.OccurrenceKey?.StartsWith("schedule:",StringComparison.Ordinal)==true)
        {
            var pieces=delivery.OccurrenceKey.Split(':');
            var schedule=pieces.Length>1 && Guid.TryParse(pieces[1],out var scheduleId) ? await db.NotificationSchedules.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==scheduleId,ct) : null;
            DateTime occurred=default;var parsed=pieces.Length==4 && DateTime.TryParseExact(pieces[2]+pieces[3],"yyyyMMddHHmm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out occurred);
            var occurrence=parsed ? BusinessTime.UtcFromLocal(occurred) : DateTime.MinValue;
            if(schedule==null || !schedule.IsActive || !parsed || (schedule.UpdatedAt.HasValue && schedule.UpdatedAt>occurrence))
            {delivery.Status="Cancelled";delivery.LastError="Schedule removed, paused or replaced";await Finish(delivery,ct);return true;}
        }
        var devices=await db.UserDeviceTokens.Where(t=>t.UserId==delivery.UserId && (delivery.AppType=="Any" || t.AppType==delivery.AppType)).ToListAsync(ct);
        if(devices.Count==0){delivery.LastError="No registered devices";delivery.AvailableAt=now.AddHours(1);await Finish(delivery,ct);return true;}
        var attempts=await db.Set<NotificationDeviceDelivery>().Where(d=>d.DeliveryId==id).ToListAsync(ct);
        var failed=false;
        foreach(var device in devices)
        {
            ct.ThrowIfCancellationRequested();
            var attempt=attempts.SingleOrDefault(a=>a.DeviceId==device.Id);
            if(attempt?.DeliveredAt!=null || attempt?.PermanentFailure==true)continue;
            if(attempt==null){attempt=new(){DeliveryId=id,DeviceId=device.Id};db.Add(attempt);attempts.Add(attempt);}
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(30));
            PushResult result;
            try{result=await transport.Send(device.Token,device.AppType,delivery.Title,delivery.Body,timeout.Token);}
            catch(OperationCanceledException) when(!ct.IsCancellationRequested){result=new(false,false,"Provider timeout");}
            db.Add(new NotificationAttempt {DeliveryId=id,DeviceId=device.Id,Timestamp=now,Outcome=result.Success ? "Delivered" : result.InvalidToken ? "InvalidToken" : "Failed",Error=result.Error});
            attempt.Attempts++;attempt.LastError=result.Error;
            if(result.Success)attempt.DeliveredAt=now;
            else if(result.InvalidToken){attempt.PermanentFailure=true;db.Remove(device);}
            else failed=true;
            await store.Save(ct);
        }
        delivery.Attempts++;
        if(!failed){var delivered=attempts.Any(a=>a.DeliveredAt!=null);delivery.Status=delivered ? "Delivered" : "Failed";delivery.DeliveredAt=delivered ? now : null;delivery.LastError=delivered ? null : "All registered device tokens were invalid";}
        else
        {
            delivery.LastError=attempts.LastOrDefault(a=>a.DeliveredAt==null && !a.PermanentFailure)?.LastError ?? "Provider failure";
            if(delivery.Attempts>=12)delivery.Status="Failed";
            delivery.AvailableAt=now.AddSeconds(Math.Min(21600,30*Math.Pow(2,Math.Min(delivery.Attempts,10))));
        }
        await Finish(delivery,ct);return true;
    }
    private async Task Finish(NotificationDelivery delivery,CancellationToken ct)
    {
        delivery.ClaimedUntil=null;delivery.ClaimId=null;await store.Save(ct);
    }
}
