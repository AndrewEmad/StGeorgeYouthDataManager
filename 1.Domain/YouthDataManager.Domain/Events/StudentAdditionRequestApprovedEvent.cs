using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public record StudentAdditionRequestApprovedEvent(StudentAdditionRequest Request, Guid AdminUserId, bool AssignedToRequestor) : IDomainEvent;
