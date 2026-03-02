using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public record StudentAssignmentRequestApprovedEvent(StudentAssignmentRequest Request, Guid AdminUserId) : IDomainEvent;
