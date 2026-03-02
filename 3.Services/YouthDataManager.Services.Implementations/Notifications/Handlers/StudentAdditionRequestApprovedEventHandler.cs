using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Events;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations.Handlers;

public class StudentAdditionRequestApprovedEventHandler : IDomainEventHandler<StudentAdditionRequestApprovedEvent>
{
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAdditionRequestApprovedEventHandler> _logger;

    public StudentAdditionRequestApprovedEventHandler(
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAdditionRequestApprovedEventHandler> logger)
    {
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAdditionRequestApprovedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            var servantId = domainEvent.Request.RequestedByUserId;
            var tokens = await _deviceTokenRepository.GetByUserIdAndAppType(servantId, "Servant");
            var tokenStrings = tokens.Select(t => t.Token).ToList();

            if (tokenStrings.Any())
            {
                var message = domainEvent.AssignedToRequestor
                    ? $"تمت إضافة {domainEvent.Request.FullName} وتخصيصه لك."
                    : $"تمت إضافة {domainEvent.Request.FullName} كطلبك (بدون تخصيص).";

                await _fcmService.SendToMultipleAsync(
                    tokenStrings,
                    "تمت الموافقة على طلبك",
                    message
                );
                _logger.LogInformation("Sent approval notification to servant {ServantId} for addition request.", servantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAdditionRequestApprovedEvent");
        }
    }
}
