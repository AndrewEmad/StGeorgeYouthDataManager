using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.FollowUp.Service.Abstractions.Commands;

public record TaskInput(Guid StudentId,string Type,DateTime DueAt,Guid? AssigneeId=null);
public record TaskChange(string Action,DateTime? DueAt,Guid? AssigneeId,string? Reason,string? Version,int? Outcome=null,string? Notes=null,DateTime? EventAt=null,List<Guid>? Participants=null);
public interface IFollowUpTaskService
{
    Task<ServiceResult<FollowUpTask>> Create(TaskInput input);
    Task<ServiceResult> Change(Guid id,TaskChange input);
}
