using YouthDataManager.Domain.Enums;

namespace YouthDataManager.Domain.Entities;

public class Student
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? SecondaryPhone { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? BirthMonth { get; set; }
    public string College { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string ConfessionFather { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? ServantId { get; set; }
    public ApplicationUser? Servant { get; set; }
    public Gender Gender { get; set; }
    public string FollowUpAreas { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? LastUpdateNotes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastAttendanceDate { get; set; }

    // Navigation properties
    public ICollection<CallLog> CallLogs { get; set; } = new List<CallLog>();
    public ICollection<HomeVisit> HomeVisits { get; set; } = new List<HomeVisit>();
    public ICollection<StudentAttendance> AttendanceRecords { get; set; } = new List<StudentAttendance>();
    public ICollection<StudentEditLog> EditLogs { get; set; } = new List<StudentEditLog>();
}
