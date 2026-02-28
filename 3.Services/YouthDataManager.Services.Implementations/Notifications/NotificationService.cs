using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Notifications.Service.Abstractions;
using YouthDataManager.Notifications.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.Notifications.Service.Implementations;

public class NotificationService : INotificationService
{
    private readonly INotificationScheduleRepository _scheduleRepo;
    private readonly IUserDeviceTokenRepository _tokenRepo;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        INotificationScheduleRepository scheduleRepo,
        IUserDeviceTokenRepository tokenRepo,
        IUnitOfWork unitOfWork)
    {
        _scheduleRepo = scheduleRepo;
        _tokenRepo = tokenRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<List<ScheduleDto>>> GetSchedules(Guid userId)
    {
        var schedules = await _scheduleRepo.GetByUserId(userId);
        var dtos = schedules.Select(s => new ScheduleDto(
            s.Id,
            s.DaysOfWeek,
            s.TimeOfDay.ToString("HH:mm"),
            s.IsActive
        )).ToList();

        return ServiceResult<List<ScheduleDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ScheduleDto>> CreateSchedule(Guid userId, CreateScheduleRequest request)
    {
        if (!TimeOnly.TryParse(request.TimeOfDay, out var timeOfDay))
            return ServiceResult<ScheduleDto>.Failure("Invalid time format. Use HH:mm.");

        // Validate days format
        var days = request.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var day in days)
        {
            if (!int.TryParse(day.Trim(), out var d) || d < 0 || d > 6)
                return ServiceResult<ScheduleDto>.Failure("Invalid day. Use comma-separated values 0-6.");
        }

        var schedule = new NotificationSchedule
        {
            UserId = userId,
            DaysOfWeek = request.DaysOfWeek,
            TimeOfDay = timeOfDay,
            IsActive = true
        };

        _scheduleRepo.Add(schedule);
        await _unitOfWork.SaveChangesAsync();

        var dto = new ScheduleDto(schedule.Id, schedule.DaysOfWeek, schedule.TimeOfDay.ToString("HH:mm"), schedule.IsActive);
        return ServiceResult<ScheduleDto>.Success(dto);
    }

    public async Task<ServiceResult> DeleteSchedule(Guid userId, Guid scheduleId)
    {
        var schedule = await _scheduleRepo.GetById(scheduleId);
        if (schedule == null || schedule.UserId != userId)
            return ServiceResult.Failure("Schedule not found.");

        _scheduleRepo.Remove(schedule);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RegisterDeviceToken(Guid userId, RegisterDeviceTokenRequest request)
    {
        var existing = await _tokenRepo.GetByToken(request.Token);
        if (existing != null)
        {
            // Update: reassign to this user and refresh timestamp
            existing.UserId = userId;
            existing.DeviceName = request.DeviceName;
            existing.LastUsedAt = DateTime.UtcNow;
            _tokenRepo.Update(existing);
        }
        else
        {
            var token = new UserDeviceToken
            {
                UserId = userId,
                Token = request.Token,
                DeviceName = request.DeviceName
            };
            _tokenRepo.Add(token);
        }

        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UnregisterDeviceToken(Guid userId, UnregisterDeviceTokenRequest request)
    {
        var existing = await _tokenRepo.GetByToken(request.Token);
        if (existing != null && existing.UserId == userId)
        {
            _tokenRepo.Remove(existing);
            await _unitOfWork.SaveChangesAsync();
        }

        return ServiceResult.Success();
    }
}
