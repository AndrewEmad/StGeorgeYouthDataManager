using System;
using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Events;

public class StudentAssignmentRequestCreatedEvent : IDomainEvent
{
    public StudentAssignmentRequest Request { get; }
    public Guid RequestedByUserId { get; }

    public StudentAssignmentRequestCreatedEvent(StudentAssignmentRequest request, Guid requestedByUserId)
    {
        Request = request;
        RequestedByUserId = requestedByUserId;
    }
}
