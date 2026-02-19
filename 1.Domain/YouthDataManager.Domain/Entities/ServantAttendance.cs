namespace YouthDataManager.Domain.Entities;

public class ServantAttendance
{
    public Guid Id { get; set; }
    public Guid ServantId { get; set; }
    public ApplicationUser Servant { get; set; } = null!;
    public DateTime Date { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
