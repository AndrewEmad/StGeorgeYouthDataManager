using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.NoTracking;

public interface INotificationScheduleNoTrackingRepository
{
    /// <summary>
    /// Gets all active schedules that match the given day and fall within the time window.
    /// </summary>
    Task<List<NotificationSchedule>> GetDueSchedules(int dayOfWeek, TimeOnly windowStart, TimeOnly windowEnd);
}
