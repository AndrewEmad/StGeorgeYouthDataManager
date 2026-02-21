using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Domain.Entities;

public class StudentAdditionRequest
{
    public Guid Id { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public AssignmentRequestStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public ApplicationUser? ProcessedByUser { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? SecondaryPhone { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; }
    public string? College { get; set; }
    public string? AcademicYear { get; set; }
    public string? ConfessionFather { get; set; }
    public string? Notes { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
}
