namespace YouthDataManager.Domain.Entities;

public class NotificationSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string DaysOfWeek { get; set; } = string.Empty; // Comma-separated: "0,1,2,3,4,5,6"
    public TimeOnly TimeOfDay { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
