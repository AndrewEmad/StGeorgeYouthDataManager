using System;

namespace YouthDataManager.Domain.Entities
{
    public class ActivityLog
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;

        public ActivityLog()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        public ActivityLog(string action, string details, string userName, string userRole) : this()
        {
            Action = action;
            Details = details;
            UserName = userName;
            UserRole = userRole;
        }
    }
}
