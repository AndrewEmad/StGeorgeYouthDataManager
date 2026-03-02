using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Events;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations.Handlers;

public class StudentAssignmentRequestCreatedEventHandler : IDomainEventHandler<StudentAssignmentRequestCreatedEvent>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAssignmentRequestCreatedEventHandler> _logger;

    public StudentAssignmentRequestCreatedEventHandler(
        UserManager<ApplicationUser> userManager,
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAssignmentRequestCreatedEventHandler> logger)
    {
        _userManager = userManager;
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAssignmentRequestCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var managers = await _userManager.GetUsersInRoleAsync("Manager");
            if (!managers.Any())
            {
                _logger.LogInformation("No managers found to notify for StudentAssignmentRequestCreatedEvent.");
                return;
            }

            var studentName = domainEvent.Request.Student?.FullName ?? "مخدوم";
            var title = "طلب تخصيص مخدوم";
            var body = $"طلب الخادم تخصيص المخدوم: {studentName}";

            foreach (var manager in managers)
            {
                var tokens = await _deviceTokenRepository.GetByUserIdAndAppType(manager.Id, "Admin");
                var tokenStrings = tokens.Select(t => t.Token).ToList();

                if (tokenStrings.Any())
                {
                    await _fcmService.SendToMultipleAsync(tokenStrings, title, body);
                    _logger.LogInformation("Sent notification to manager {ManagerId} for new assignment request.", manager.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAssignmentRequestCreatedEvent.");
        }
    }
}
