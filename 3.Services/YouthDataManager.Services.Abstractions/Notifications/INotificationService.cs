using YouthDataManager.Notifications.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Abstractions;

public interface INotificationService
{
    Task<ServiceResult<List<ScheduleDto>>> GetSchedules(Guid userId);
    Task<ServiceResult<ScheduleDto>> CreateSchedule(Guid userId, CreateScheduleRequest request);
    Task<ServiceResult> DeleteSchedule(Guid userId, Guid scheduleId);
    Task<ServiceResult> RegisterDeviceToken(Guid userId, RegisterDeviceTokenRequest request);
    Task<ServiceResult> UnregisterDeviceToken(Guid userId, UnregisterDeviceTokenRequest request);
}
