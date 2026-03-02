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

public class StudentAdditionRequestCreatedEventHandler : IDomainEventHandler<StudentAdditionRequestCreatedEvent>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly IFcmService _fcmService;
    private readonly ILogger<StudentAdditionRequestCreatedEventHandler> _logger;

    public StudentAdditionRequestCreatedEventHandler(
        UserManager<ApplicationUser> userManager,
        IUserDeviceTokenRepository deviceTokenRepository,
        IFcmService fcmService,
        ILogger<StudentAdditionRequestCreatedEventHandler> logger)
    {
        _userManager = userManager;
        _deviceTokenRepository = deviceTokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }

    public async Task Handle(StudentAdditionRequestCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var managers = await _userManager.GetUsersInRoleAsync("Manager");
            if (!managers.Any())
            {
                _logger.LogInformation("No managers found to notify for StudentAdditionRequestCreatedEvent.");
                return;
            }

            var title = "طلب إضافة مخدوم جديد";
            var body = $"طلب الخادم إضافة المخدوم: {domainEvent.Request.FullName}";

            foreach (var manager in managers)
            {
                var tokens = await _deviceTokenRepository.GetByUserIdAndAppType(manager.Id, "Admin");
                var tokenStrings = tokens.Select(t => t.Token).ToList();

                if (tokenStrings.Any())
                {
                    await _fcmService.SendToMultipleAsync(tokenStrings, title, body);
                    _logger.LogInformation("Sent notification to manager {ManagerId} for new addition request.", manager.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling StudentAdditionRequestCreatedEvent.");
        }
    }
}
