using Microsoft.AspNetCore.Identity;

namespace YouthDataManager.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<CallLog> CallHistory { get; set; } = new List<CallLog>();
    public ICollection<HomeVisit> VisitHistory { get; set; } = new List<HomeVisit>();
}
