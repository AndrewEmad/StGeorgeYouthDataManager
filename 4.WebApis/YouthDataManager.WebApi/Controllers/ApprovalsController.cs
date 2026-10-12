using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController,Route("api/approvals"),Authorize]
public sealed class ApprovalsController(IApplicationStore store,ResourceAccess access,ICallerContext caller) : ControllerBase
{
    [HttpGet("{kind}")]
    public async Task<IActionResult> History(string kind,string? status=null,bool mine=true,Guid? requestId=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        status=string.IsNullOrWhiteSpace(status) ? null : status;
        if (!mine && !access.Privileged) return Forbid();
        if (kind is not ("all" or "addition" or "assignment" or "removal") || requestId==Guid.Empty || page<1 || pageSize is <1 or >100 || (status!=null && status is not ("Pending" or "Approved" or "Rejected" or "Cancelled"))) return BadRequest("Invalid history filters.");
        if ((long)(page-1)*pageSize>int.MaxValue)return BadRequest("Invalid page offset.");
        var additions=store.Query<StudentAdditionRequest>(false,true);
        var assignments=store.Query<StudentAssignmentRequest>(false,true);
        var removals=store.Query<StudentRemovalRequest>(false,true);
        if(mine){additions=additions.Where(r=>r.RequestedByUserId==access.UserId);assignments=assignments.Where(r=>r.RequestedByUserId==access.UserId);removals=removals.Where(r=>r.RequestedByUserId==access.UserId);}
        if(requestId.HasValue){additions=additions.Where(r=>r.Id==requestId);assignments=assignments.Where(r=>r.Id==requestId);removals=removals.Where(r=>r.Id==requestId);}
        if(status!=null){var parsed=Enum.Parse<AssignmentRequestStatus>(status);var removalStatus=Enum.Parse<RemovalRequestStatus>(status);additions=additions.Where(r=>r.Status==parsed);assignments=assignments.Where(r=>r.Status==parsed);removals=removals.Where(r=>r.Status==removalStatus);}
        IQueryable<ApprovalIndexRow> index=kind switch
        {
            "addition"=>additions.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="addition",RequestedAt=r.RequestedAt}),
            "assignment"=>assignments.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="assignment",RequestedAt=r.RequestedAt}),
            "removal"=>removals.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="removal",RequestedAt=r.RequestedAt}),
            _=>additions.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="addition",RequestedAt=r.RequestedAt})
                .Concat(assignments.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="assignment",RequestedAt=r.RequestedAt}))
                .Concat(removals.Select(r=>new ApprovalIndexRow {Id=r.Id,Kind="removal",RequestedAt=r.RequestedAt}))
        };
        // Count/order/page the combined feed in SQL, then fetch at most one bounded projection per request type.
        var count=await index.CountAsync(ct);
        var selected=await index.OrderByDescending(r=>r.RequestedAt).ThenBy(r=>r.Id).ThenBy(r=>r.Kind).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        var rows=new List<ApprovalHistoryRow>();
        var ids=selected.Where(r=>r.Kind=="addition").Select(r=>r.Id).ToArray();
        if(ids.Length>0)rows.AddRange(await AdditionRows(additions.Where(r=>ids.Contains(r.Id))).ToListAsync(ct));
        ids=selected.Where(r=>r.Kind=="assignment").Select(r=>r.Id).ToArray();
        if(ids.Length>0)rows.AddRange(await AssignmentRows(assignments.Where(r=>ids.Contains(r.Id))).ToListAsync(ct));
        ids=selected.Where(r=>r.Kind=="removal").Select(r=>r.Id).ToArray();
        if(ids.Length>0)rows.AddRange(await RemovalRows(removals.Where(r=>ids.Contains(r.Id))).ToListAsync(ct));
        var byKey=rows.ToDictionary(r=>(r.Kind,r.Id));
        return Ok(new {Items=selected.Where(r=>byKey.ContainsKey((r.Kind,r.Id))).Select(r=>byKey[(r.Kind,r.Id)]),TotalCount=count,Page=page,PageSize=pageSize});
    }
    private IQueryable<ApprovalHistoryRow> AdditionRows(IQueryable<StudentAdditionRequest> query)
    {
        var users=store.Query<ApplicationUser>(false);
        return from r in query join u in users on r.RequestedByUserId equals u.Id
            select new ApprovalHistoryRow(r.Id,r.RequestedByUserId,u.FullName,r.RequestedAt,r.Status.ToString(),r.ProcessedAt,r.ProcessedByUserId,r.ProcessingReason,r.CreatedStudentId,r.FullName,r.Phone,r.SecondaryPhone,r.Address,r.Area,r.College,r.AcademicYear,r.ConfessionFather,r.BirthDate,r.BirthMonth,r.Notes,(int)r.Gender,null,r.Version,r.CreatedStudentId,users.Where(u=>u.Id==r.ProcessedByUserId).Select(u=>u.FullName).FirstOrDefault()) {Kind="addition"};
    }
    private IQueryable<ApprovalHistoryRow> AssignmentRows(IQueryable<StudentAssignmentRequest> query)
    {
        var users=store.Query<ApplicationUser>(false);
        return from r in query join u in users on r.RequestedByUserId equals u.Id
            select new ApprovalHistoryRow(r.Id,r.RequestedByUserId,u.FullName,r.RequestedAt,r.Status.ToString(),r.ProcessedAt,r.ProcessedByUserId,r.ProcessingReason,r.StudentId,r.Student.FullName,null,null,null,null,null,null,null,null,null,null,null,null,r.Version,null,users.Where(u=>u.Id==r.ProcessedByUserId).Select(u=>u.FullName).FirstOrDefault()) {Kind="assignment"};
    }
    private IQueryable<ApprovalHistoryRow> RemovalRows(IQueryable<StudentRemovalRequest> query)
    {
        var users=store.Query<ApplicationUser>(false);
        return from r in query join u in users on r.RequestedByUserId equals u.Id
            select new ApprovalHistoryRow(r.Id,r.RequestedByUserId,u.FullName,r.RequestedAt,r.Status.ToString(),r.ProcessedAt,r.ProcessedByUserId,r.ProcessingReason,r.StudentId,r.Student.FullName,null,null,null,null,null,null,null,null,null,null,null,(int)r.RemovalType,r.Version,null,users.Where(u=>u.Id==r.ProcessedByUserId).Select(u=>u.FullName).FirstOrDefault()) {Kind="removal"};
    }
    private sealed class ApprovalIndexRow {public Guid Id {get;init;} public string Kind {get;init;}=""; public DateTime RequestedAt {get;init;}}
    [HttpPost("{kind}/{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(string kind,Guid id,Withdrawal input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length>1000)return BadRequest("A withdrawal reason (1–1000 characters) is required.");
        var now=caller.UtcNow;var reason=string.IsNullOrWhiteSpace(input.Reason) ? "Withdrawn by requester" : input.Reason.Trim();
        if (kind=="addition")
        {
            var r=await store.Query<StudentAdditionRequest>(true,true).SingleOrDefaultAsync(r=>r.Id==id,ct);
            if (r==null) return NotFound();if(r.RequestedByUserId!=access.UserId) return Forbid();if(r.Status==AssignmentRequestStatus.Cancelled)return Ok();if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(r.Version))return Conflict("Request changed.");if(r.Status!=AssignmentRequestStatus.Pending) return Conflict("Request already processed.");
            r.Status=AssignmentRequestStatus.Cancelled;r.ProcessingReason=reason;r.ProcessedAt=now;r.ProcessedByUserId=access.UserId;
        }
        else if(kind=="assignment")
        {
            var r=await store.Query<StudentAssignmentRequest>(true,true).SingleOrDefaultAsync(r=>r.Id==id,ct);
            if (r==null) return NotFound();if(r.RequestedByUserId!=access.UserId) return Forbid();if(r.Status==AssignmentRequestStatus.Cancelled)return Ok();if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(r.Version))return Conflict("Request changed.");if(r.Status!=AssignmentRequestStatus.Pending) return Conflict("Request already processed.");
            r.Status=AssignmentRequestStatus.Cancelled;r.ProcessingReason=reason;r.ProcessedAt=now;r.ProcessedByUserId=access.UserId;
        }
        else if(kind=="removal")
        {
            var r=await store.Query<StudentRemovalRequest>(true,true).SingleOrDefaultAsync(r=>r.Id==id,ct);
            if (r==null) return NotFound();if(r.RequestedByUserId!=access.UserId) return Forbid();if(r.Status==RemovalRequestStatus.Cancelled)return Ok();if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(r.Version))return Conflict("Request changed.");if(r.Status!=RemovalRequestStatus.Pending) return Conflict("Request already processed.");
            r.Status=RemovalRequestStatus.Cancelled;r.ProcessingReason=reason;r.ProcessedAt=now;r.ProcessedByUserId=access.UserId;
        }
        else return BadRequest("Invalid request kind.");
        await store.Save(ct);return Ok();
    }
}
public record Withdrawal(string? Reason);
public record ApprovalHistoryRow(Guid Id,Guid RequestedByUserId,string RequestedByUserName,DateTime RequestedAt,string Status,DateTime? ProcessedAt,Guid? ProcessedByUserId,string? ProcessingReason,Guid? StudentId,string FullName,string? Phone,string? SecondaryPhone,string? Address,string? Area,string? College,string? AcademicYear,string? ConfessionFather,DateTime? BirthDate,int? BirthMonth,string? Notes,int? Gender,int? RemovalType,byte[] Version,Guid? CreatedStudentId,string? ProcessedByUserName) { public string Kind {get;init;}=""; }
