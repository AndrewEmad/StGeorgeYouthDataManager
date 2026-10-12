namespace YouthDataManager.Domain.Entities;

public class RefreshSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public string SecurityStamp { get; set; } = "";
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public byte[] Version { get; set; } = [];
}
public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Action { get; set; } = "";
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string CorrelationId { get; set; } = "";
    public string? Reason { get; set; }
}
public class FollowUpTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string Type { get; set; } = "Call";
    public string Status { get; set; } = "Open";
    public DateTime DueAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedRecordId { get; set; }
    public Guid? SourceId { get; set; }
    public string? SourceType { get; set; }
    public string? Reason { get; set; }
    public byte[] Version { get; set; } = [];
}
public class MeetingSession
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public string Status { get; set; } = "Draft";
    public bool RosterKnown { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAt { get; set; }
    public byte[] Version { get; set; } = [];
}
public class MeetingConfiguration
{
    public int Id { get; set; } = 1;
    public int MeetingDay { get; set; } = 5;
    public int ReportWeekStart { get; set; } = 5;
    public byte[] Version { get; set; } = [];
}
public class MeetingRosterEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid PersonId { get; set; }
    public string Kind { get; set; } = "Student";
    public string Name { get; set; } = "";
    public Guid? AssignedServantId { get; set; }
    public bool Present { get; set; }
}
public class NotificationDelivery
{
    public string Status { get; set; } = "Pending";
    public Guid? ClaimId { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string AppType { get; set; } = "Servant";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? OccurrenceKey { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ClaimedUntil { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public byte[] Version { get; set; } = [];
}
public class ImportRowOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperationId { get; set; }
    public Guid ActorId { get; set; }
    public int Row { get; set; }
    public string PayloadHash { get; set; } = "";
    public Guid? StudentId { get; set; }
    public string? Error { get; set; }
}

public class NotificationDeviceDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool PermanentFailure { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
public class WorkerCheckpoint
{
    public string Name { get; set; } = "";
    public DateTime CompletedThrough { get; set; }
}

public class NotificationAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryId { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Outcome { get; set; } = "Failed";
    public string? Error { get; set; }
}
