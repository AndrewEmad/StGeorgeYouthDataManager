using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Students.Service.Abstractions.Commands;

public interface IMeetingService
{
    Task<ServiceResult<MeetingSession>> Start(DateTime date);
    Task<ServiceResult> Mark(Guid sessionId, string kind, IReadOnlyList<Guid> ids, bool present, string? reason);
    Task<ServiceResult> ChangeStatus(Guid sessionId, string status, string? reason);
    Task<ServiceResult> ReviewEligibility(Guid sessionId,IReadOnlyList<Guid> students,IReadOnlyList<Guid> servants,string reason);
}
