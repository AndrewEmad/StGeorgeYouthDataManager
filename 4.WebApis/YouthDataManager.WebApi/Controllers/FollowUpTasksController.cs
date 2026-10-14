using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[PersonalWorkspace]
[ApiController,Route("api/follow-up-tasks"),Authorize]
public sealed class FollowUpTasksController(IFollowUpTaskService service,IApplicationStore store,ResourceAccess access) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? status="Open",string? type=null,bool unassigned=false,Guid? studentId=null,Guid? taskId=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        status=string.IsNullOrWhiteSpace(status) ? null : status; type=string.IsNullOrWhiteSpace(type) ? null : type;
        if (page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue || (status!=null && status is not ("Open" or "Completed" or "Cancelled" or "PendingReview")) || (type!=null && type is not ("Call" or "Visit"))) return BadRequest("Invalid task filters.");
        if (unassigned && !access.Privileged) return Forbid();
        var q=store.Query<FollowUpTask>(false).Where(t=>access.Privileged || t.AssigneeId==access.UserId);
        if(taskId.HasValue)q=q.Where(t=>t.Id==taskId);
        if (unassigned) q=q.Where(t=>t.AssigneeId==null);
        if (status!=null) q=q.Where(t=>t.Status==status);
        if (type!=null) q=q.Where(t=>t.Type==type);
        if (studentId.HasValue) q=q.Where(t=>t.StudentId==studentId);
        var students=store.Query<Student>(false,true); var users=store.Query<ApplicationUser>(false);
        var rows=from t in q join s in students on t.StudentId equals s.Id join u in users on t.AssigneeId equals u.Id into owners from u in owners.DefaultIfEmpty()
                 select new {t.Id,t.StudentId,StudentName=s.FullName,t.AssigneeId,AssigneeName=u==null ? null : u.FullName,t.Type,t.Status,t.DueAt,t.CreatedAt,t.CompletedAt,t.SourceType,t.SourceId,t.Reason,t.Version};
        var now=DateTime.UtcNow;
        return Ok(new { Items=await rows.OrderBy(t=>t.DueAt).ThenBy(t=>t.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct),TotalCount=await q.CountAsync(ct),DueStudents=await q.Where(t=>t.Status=="Open" && t.DueAt<=now).Select(t=>t.StudentId).Distinct().CountAsync(ct),Page=page,PageSize=pageSize });
    }
    [HttpPost]
    public async Task<IActionResult> Create(TaskInput input)
    {
        var r=await service.Create(input); return r.Status==ServiceResultStatus.Success ? Ok(r.Data) : Result(r);
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Change(Guid id,TaskChange input) => Result(await service.Change(id,input));
    private IActionResult Result(ServiceResult r) => r.Status==ServiceResultStatus.Success ? Ok() : r.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : r.Errors.Any(e=>e.Code=="Conflict") ? Conflict(r) : BadRequest(r);
}
