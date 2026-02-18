using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Domain.Entities;

public class CallLog
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public Guid ServantId { get; set; }
    public ApplicationUser Servant { get; set; } = null!;
    public DateTime CallDate { get; set; }
    public CallStatus CallStatus { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime? NextFollowUpDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
