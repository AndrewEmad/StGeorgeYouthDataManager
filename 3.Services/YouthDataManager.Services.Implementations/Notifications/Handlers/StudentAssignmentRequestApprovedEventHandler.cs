using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Events;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations.Handlers;

public class StudentAssignmentRequestApprovedEventHandler : IDomainEventHandler<StudentAssignmentRequestApprovedEvent>
{
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAssignmentRequestApprovedEventHandler> _logger;

    public StudentAssignmentRequestApprovedEventHandler(
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAssignmentRequestApprovedEventHandler> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAssignmentRequestApprovedEvent domainEvent, CancellationToken cancellationToken)
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
                    "تمت الموافقة على طلبك",
                    $"تمت الموافقة على طلب تخصيص المخدوم: {domainEvent.Request.Student?.FullName ?? "مخدوم"}"
                );
                _logger.LogInformation("Sent approval notification to servant {ServantId} for assignment request.", servantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAssignmentRequestApprovedEvent");
        }
    }
}
