using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.Tracking;

public class NotificationScheduleRepository : INotificationScheduleRepository
{
    private readonly AppDbContext _context;

    public NotificationScheduleRepository(AppDbContext context) => _context = context;

    public async Task<NotificationSchedule?> GetById(Guid id) =>
        await _context.NotificationSchedules.FindAsync(id);

    public async Task<List<NotificationSchedule>> GetByUserId(Guid userId) =>
        await _context.NotificationSchedules
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.TimeOfDay)
            .ToListAsync();

    public void Add(NotificationSchedule schedule) => _context.NotificationSchedules.Add(schedule);

    public void Update(NotificationSchedule schedule) => _context.NotificationSchedules.Update(schedule);

    public void Remove(NotificationSchedule schedule) => _context.NotificationSchedules.Remove(schedule);
}
