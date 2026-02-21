using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Domain.Entities;

public class StudentRemovalRequest
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public Guid RequestedByUserId { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public RemovalRequestStatus Status { get; set; }
    public RemovalRequestType RemovalType { get; set; } = RemovalRequestType.DeleteFromSystem;
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public ApplicationUser? ProcessedByUser { get; set; }
}
