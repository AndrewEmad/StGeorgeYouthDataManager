using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Events;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations.Handlers;

public class StudentAssignmentRequestRejectedEventHandler : IDomainEventHandler<StudentAssignmentRequestRejectedEvent>
{
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAssignmentRequestRejectedEventHandler> _logger;

    public StudentAssignmentRequestRejectedEventHandler(
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAssignmentRequestRejectedEventHandler> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAssignmentRequestRejectedEvent domainEvent, CancellationToken cancellationToken)
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
                    $"تم رفض طلب تخصيص المخدوم: {domainEvent.Request.Student?.FullName ?? "مخدوم"}"
                );
                _logger.LogInformation("Sent rejection notification to servant {ServantId} for assignment request.", servantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAssignmentRequestRejectedEvent");
        }
    }
}
