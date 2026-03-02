using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public record StudentAssignmentRequestRejectedEvent(StudentAssignmentRequest Request, Guid AdminUserId) : IDomainEvent;
