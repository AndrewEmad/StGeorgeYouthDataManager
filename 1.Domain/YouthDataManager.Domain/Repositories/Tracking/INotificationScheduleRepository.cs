using YouthDataManager.Domain.Entities;

namespace YouthDataManager.Domain.Repositories.Tracking;

public interface INotificationScheduleRepository
{
    Task<NotificationSchedule?> GetById(Guid id);
    Task<List<NotificationSchedule>> GetByUserId(Guid userId);
    void Add(NotificationSchedule schedule);
    void Update(NotificationSchedule schedule);
    void Remove(NotificationSchedule schedule);
}
