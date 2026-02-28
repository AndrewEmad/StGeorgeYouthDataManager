using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.Infrastructure.Repositories.NoTracking;

public class NotificationScheduleNoTrackingRepository : INotificationScheduleNoTrackingRepository
{
    private readonly AppDbContext _context;

    public NotificationScheduleNoTrackingRepository(AppDbContext context) => _context = context;

    public async Task<List<NotificationSchedule>> GetDueSchedules(int dayOfWeek, TimeOnly windowStart, TimeOnly windowEnd)
    {
        var dayStr = dayOfWeek.ToString();

        return await _context.NotificationSchedules
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => s.IsActive
                && s.DaysOfWeek.Contains(dayStr)
                && s.TimeOfDay >= windowStart
                && s.TimeOfDay < windowEnd)
            .ToListAsync();
    }
}
