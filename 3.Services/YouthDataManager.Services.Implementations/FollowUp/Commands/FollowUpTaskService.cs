using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Implementations.Commands;

public sealed class FollowUpTaskService(IApplicationStore store,ICallerContext caller,IFollowUpCommandsService records) : IFollowUpTaskService
{
    public Task<ServiceResult<FollowUpTask>> Create(TaskInput input) => store.Transaction(async () =>
    {
        var s=await store.Query<Student>().SingleOrDefaultAsync(s=>s.Id==input.StudentId, caller.CancellationToken);
        if (s==null) return ServiceResult<FollowUpTask>.Failure("Student not found.");
        if (!caller.IsPrivileged && s.ServantId!=caller.UserId) return ServiceResult<FollowUpTask>.Failure("Not authorized.",new ServiceError("Forbidden"));
        var owner=input.AssigneeId ?? s.ServantId;
        if (!caller.IsPrivileged && owner!=caller.UserId) return ServiceResult<FollowUpTask>.Failure("Not authorized.",new ServiceError("Forbidden"));
        if (owner.HasValue && !await EligibleUsers.Query(store).AnyAsync(u=>u.Id==owner, caller.CancellationToken)) return ServiceResult<FollowUpTask>.Failure("Assignee is inactive or ineligible.");
        if (input.Type is not ("Call" or "Visit") || input.DueAt.Year<2000 || input.DueAt.Year>2100) return ServiceResult<FollowUpTask>.Failure("Invalid task type or due date.");
        var task=new FollowUpTask { StudentId=s.Id,AssigneeId=owner,Type=input.Type,DueAt=input.DueAt.ToUniversalTime(),SourceType="Manual" };
        store.Add(task); return ServiceResult<FollowUpTask>.Success(task);
    },r=>r.Status==ServiceResultStatus.Success);

    public Task<ServiceResult> Change(Guid id,TaskChange input) => store.Transaction(async () =>
    {
        var task=await store.Query<FollowUpTask>().SingleOrDefaultAsync(t=>t.Id==id, caller.CancellationToken);
        if (task==null) return ServiceResult.Failure("Task not found.");
        if (!caller.IsPrivileged && task.AssigneeId!=caller.UserId) return ServiceResult.Failure("Not authorized.",new ServiceError("Forbidden"));
        if (input.Version==null || input.Version!=Convert.ToBase64String(task.Version)) return ServiceResult.Failure("Task changed. Refresh before retrying.",new ServiceError("Conflict"));
        if (task.Status is "Completed" or "Cancelled") return ServiceResult.Failure("Task already processed.",new ServiceError("Conflict"));
        var student=await store.Query<Student>().SingleOrDefaultAsync(s=>s.Id==task.StudentId, caller.CancellationToken);
        if (student==null) return ServiceResult.Failure("Student archived.",new ServiceError("Conflict"));
        if (!caller.IsPrivileged && student.ServantId!=caller.UserId) return ServiceResult.Failure("Ownership changed.",new ServiceError("Conflict"));
        switch (input.Action)
        {
            case "Reassign":
                if (!caller.IsPrivileged) return ServiceResult.Failure("Not authorized.",new ServiceError("Forbidden"));
                if (input.AssigneeId.HasValue && !await EligibleUsers.Query(store).AnyAsync(u=>u.Id==input.AssigneeId, caller.CancellationToken)) return ServiceResult.Failure("Assignee is inactive or ineligible.");
                task.AssigneeId=input.AssigneeId; break;
            case "Reschedule":
                if (!input.DueAt.HasValue || input.DueAt.Value.Year<2000 || input.DueAt.Value.Year>2100) return ServiceResult.Failure("Invalid due date.");
                task.DueAt=input.DueAt.Value.ToUniversalTime(); break;
            case "Activate":
                if (!caller.IsPrivileged || task.Status!="PendingReview") return ServiceResult.Failure("Pending review requires a privileged reviewer.",new ServiceError("Forbidden"));
                if (!task.AssigneeId.HasValue || !await EligibleUsers.Query(store).AnyAsync(u=>u.Id==task.AssigneeId, caller.CancellationToken)) return ServiceResult.Failure("Assign an eligible owner before activation.");
                task.Status="Open"; break;
            case "Cancel":
                if (string.IsNullOrWhiteSpace(input.Reason)) return ServiceResult.Failure("Cancellation reason is required.");
                task.Status="Cancelled"; break;
            case "Complete":
                if (task.Status!="Open" || !input.Outcome.HasValue || !input.EventAt.HasValue || input.EventAt.Value.Year<2000) return ServiceResult.Failure("Provide a valid completed activity.");
                var actor=caller.UserId ?? Guid.Empty;
                var result=task.Type=="Call"
                    ? await records.RegisterCall(new CreateCallLogRequest(task.StudentId,input.EventAt.Value,(CallStatus)input.Outcome.Value,input.Notes ?? "",null),actor)
                    : await records.RegisterVisit(new CreateHomeVisitRequest(task.StudentId,input.EventAt.Value,(VisitOutcome)input.Outcome.Value,input.Notes ?? "",null,input.Participants),actor);
                if (result.Status!=ServiceResultStatus.Success) return result;
                task.Status="Completed"; task.CompletedAt=DateTime.UtcNow; task.CompletedRecordId=result.Data; break;
            default: return ServiceResult.Failure("Invalid task action.");
        }
        task.Reason=input.Reason?.Trim();
        return ServiceResult.Success();
    },r=>r.Status==ServiceResultStatus.Success);
}
