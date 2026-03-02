using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Events;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations.Handlers;

public class StudentAdditionRequestRejectedEventHandler : IDomainEventHandler<StudentAdditionRequestRejectedEvent>
{
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAdditionRequestRejectedEventHandler> _logger;

    public StudentAdditionRequestRejectedEventHandler(
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAdditionRequestRejectedEventHandler> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAdditionRequestRejectedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            var servantId = domainEvent.Request.RequestedByUserId;
            var tokens = await _deviceTokenRepository.GetByUserIdAndAppType(servantId, "Servant");
            var tokenStrings = tokens.Select(t => t.Token).ToList();

            if (tokenStrings.Any())
            {
                await _fcmService.SendToMultipleAsync(
                    tokenStrings,
                    "تم رفض طلبك",
                    $"تم رفض طلب إضافة المخدوم: {domainEvent.Request.FullName}"
                );
                _logger.LogInformation("Sent rejection notification to servant {ServantId} for addition request.", servantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAdditionRequestRejectedEvent");
        }
    }
}
