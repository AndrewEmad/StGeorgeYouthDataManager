using System;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public class StudentAdditionRequestCreatedEvent : IDomainEvent
{
    public StudentAdditionRequest Request { get; }
    public Guid RequestedByUserId { get; }

    public StudentAdditionRequestCreatedEvent(StudentAdditionRequest request, Guid requestedByUserId)
    {
        Request = request;
        RequestedByUserId = requestedByUserId;
    }
}
