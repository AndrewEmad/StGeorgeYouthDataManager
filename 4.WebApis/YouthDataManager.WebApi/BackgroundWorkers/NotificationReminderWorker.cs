using YouthDataManager.Domain.Repositories.NoTracking;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.WebApi.BackgroundWorkers;

public class NotificationReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationReminderWorker> _logger;
    private readonly IConfiguration _configuration;

    private static readonly TimeZoneInfo EgyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

    public NotificationReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationReminderWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationReminderWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = _configuration.GetValue("NotificationWorker:IntervalMinutes", 15);

            try
            {
                await CheckAndSendNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationReminderWorker cycle.");
            }

            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }

        _logger.LogInformation("NotificationReminderWorker stopped.");
    }

    private async Task CheckAndSendNotifications(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var scheduleRepo = scope.ServiceProvider.GetRequiredService<INotificationScheduleNoTrackingRepository>();
        var tokenRepo = scope.ServiceProvider.GetRequiredService<IUserDeviceTokenRepository>();
        var fcmService = scope.ServiceProvider.GetRequiredService<IFcmService>();

        // Get current Egypt time
        var utcNow = DateTime.UtcNow;
        var egyptNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, EgyptTimeZone);
        var currentDay = (int)egyptNow.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(egyptNow);

        var intervalMinutes = _configuration.GetValue("NotificationWorker:IntervalMinutes", 15);
        var windowStart = currentTime.AddMinutes(-intervalMinutes);
        var windowEnd = currentTime;

        _logger.LogInformation(
            "Checking notifications: Egypt time {EgyptTime}, Day {Day}, Window {Start}-{End}",
            egyptNow, currentDay, windowStart, windowEnd);

        var dueSchedules = await scheduleRepo.GetDueSchedules(currentDay, windowStart, windowEnd);

        if (dueSchedules.Count == 0)
        {
            _logger.LogInformation("No due notification schedules found.");
            return;
        }

        _logger.LogInformation("Found {Count} due schedules.", dueSchedules.Count);

        // Group by user to avoid sending multiple notifications to the same user
        var userGroups = dueSchedules.GroupBy(s => s.UserId);

        foreach (var group in userGroups)
        {
            var userId = group.Key;
            var tokens = await tokenRepo.GetByUserId(userId);

            if (tokens.Count == 0)
            {
                _logger.LogInformation("User {UserId} has no device tokens, skipping.", userId);
                continue;
            }

            var tokenStrings = tokens.Select(t => t.Token).ToList();
            var sent = await fcmService.SendToMultipleAsync(
                tokenStrings,
                "تذكير متابعة",
                "وقت متابعة المخدومين 📞");

            _logger.LogInformation(
                "Sent reminder to user {UserId}: {Sent}/{Total} devices succeeded.",
                userId, sent, tokenStrings.Count);
        }
    }
}
