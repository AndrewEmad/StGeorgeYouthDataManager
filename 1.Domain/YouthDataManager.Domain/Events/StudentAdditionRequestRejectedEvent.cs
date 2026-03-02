using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public record StudentAdditionRequestRejectedEvent(StudentAdditionRequest Request, Guid AdminUserId) : IDomainEvent;
