namespace YouthDataManager.Domain.Entities;

public class StudentEditLog
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public string UpdatedByUserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
