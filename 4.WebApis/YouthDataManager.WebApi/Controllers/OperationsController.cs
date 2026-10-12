using YouthDataManager.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController,Route("api/operations"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
public sealed class OperationsController(AppDbContext db,LegacyBackfill backfill,DatabaseTelemetry databaseTelemetry,YouthDataManager.WebApi.Operations.RequestTelemetry requestTelemetry) : ControllerBase
{
    [HttpPost("backfill")]
    public async Task<IActionResult> Backfill(bool dryRun=true,int batchSize=100,CancellationToken ct=default) => Ok(await backfill.Run(dryRun,batchSize,ct));
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var now=DateTime.UtcNow;
        return Ok(new {PendingMigrations=await db.Database.GetPendingMigrationsAsync(ct),PendingDeliveries=await db.Set<NotificationDelivery>().CountAsync(d=>d.Status=="Pending",ct),FailedDeliveries=await db.Set<NotificationDelivery>().CountAsync(d=>d.Status=="Failed",ct),OldestPending=await db.Set<NotificationDelivery>().Where(d=>d.Status=="Pending").Select(d=>(DateTime?)d.AvailableAt).MinAsync(ct),Worker=await db.Set<WorkerCheckpoint>().AsNoTracking().ToListAsync(ct),PendingReviewTasks=await db.Set<FollowUpTask>().CountAsync(t=>t.Status=="PendingReview",ct)});
    }
    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics(CancellationToken ct)
    {
        var now=DateTime.UtcNow;var oldest=await db.Set<NotificationDelivery>().Where(d=>d.Status=="Pending").Select(d=>(DateTime?)d.AvailableAt).MinAsync(ct);
        var checkpoint=await db.Set<WorkerCheckpoint>().Where(w=>w.Name=="NotificationSchedule").Select(w=>(DateTime?)w.CompletedThrough).SingleOrDefaultAsync(ct);
        return Ok(new {CapturedAt=now,Api=requestTelemetry.Snapshot(),Database=databaseTelemetry.Snapshot(),WorkerLagSeconds=checkpoint.HasValue ? Math.Max(0,(now-checkpoint.Value).TotalSeconds) : (double?)null,DeliveryLagSeconds=oldest.HasValue ? Math.Max(0,(now-oldest.Value).TotalSeconds) : 0,FailedDeliveries=await db.Set<NotificationDelivery>().CountAsync(d=>d.Status=="Failed",ct),PendingMigrations=(await db.Database.GetPendingMigrationsAsync(ct)).Count()});
    }
    [HttpGet("deliveries")]
    public async Task<IActionResult> Deliveries(string? status=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if(page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue || (status!=null && status is not ("Pending" or "Delivered" or "Cancelled" or "Failed")))return BadRequest("Invalid filters.");
        var q=db.Set<NotificationDelivery>().AsNoTracking();if(status!=null)q=q.Where(d=>d.Status==status);
        return Ok(new {TotalCount=await q.CountAsync(ct),Items=await q.OrderByDescending(d=>d.AvailableAt).ThenBy(d=>d.Id).Skip((page-1)*pageSize).Take(pageSize).Select(d=>new {d.Id,d.UserId,d.Status,d.Title,d.AvailableAt,d.DeliveredAt,d.Attempts,d.LastError}).ToListAsync(ct)});
    }
    [HttpPost("deliveries/{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id,CancellationToken ct)
    {
        var d=await db.Set<NotificationDelivery>().SingleOrDefaultAsync(d=>d.Id==id,ct);if(d==null)return NotFound();
        if(d.Status!="Failed")return Conflict("Only failed deliveries can be retried.");
        d.Status="Pending";d.Attempts=0;d.AvailableAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);return Ok();
    }
}
