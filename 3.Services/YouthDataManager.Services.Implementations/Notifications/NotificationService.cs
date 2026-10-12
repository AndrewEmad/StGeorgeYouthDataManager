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
        if(!ParseSchedule(request.DaysOfWeek,request.TimeOfDay,out var canonical,out var timeOfDay))
            return ServiceResult<ScheduleDto>.Failure("Choose at least one day (0-6) and a valid HH:mm time.");

        var schedule = new NotificationSchedule
        {
            UserId = userId,
            DaysOfWeek = canonical,
            TimeOfDay = timeOfDay,
            IsActive = true
        };

        _scheduleRepo.Add(schedule);
        await _unitOfWork.SaveChangesAsync();

        var dto = new ScheduleDto(schedule.Id, schedule.DaysOfWeek, schedule.TimeOfDay.ToString("HH:mm"), schedule.IsActive);
        return ServiceResult<ScheduleDto>.Success(dto);
    }

    private static bool ParseSchedule(string days,string time,out string canonical,out TimeOnly value)
    {
        canonical="";value=default;
        if(string.IsNullOrWhiteSpace(days) || !TimeOnly.TryParseExact(time,"HH:mm",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out value))return false;
        var parts=days.Split(',');var parsed=new HashSet<int>();
        foreach(var part in parts){if(!int.TryParse(part.Trim(),out var day) || day is <0 or >6)return false;parsed.Add(day);}
        canonical=string.Join(',',parsed.Order());return parsed.Count>0;
    }
    public async Task<ServiceResult<ScheduleDto>> UpdateSchedule(Guid userId,Guid scheduleId,UpdateScheduleRequest request)
    {
        if(!ParseSchedule(request.DaysOfWeek,request.TimeOfDay,out var days,out var time))return ServiceResult<ScheduleDto>.Failure("Choose at least one day (0-6) and a valid HH:mm time.");
        var schedule=await _scheduleRepo.GetById(scheduleId);
        if(schedule==null || schedule.UserId!=userId)return ServiceResult<ScheduleDto>.Failure("Schedule not found.");
        schedule.DaysOfWeek=days;schedule.TimeOfDay=time;schedule.IsActive=request.IsActive;schedule.UpdatedAt=DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();return ServiceResult<ScheduleDto>.Success(new(schedule.Id,days,time.ToString("HH:mm"),schedule.IsActive));
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
        if(string.IsNullOrWhiteSpace(request.Token) || request.Token.Length>4096 || request.AppType is not ("Admin" or "Servant") || request.DeviceName?.Length>200)return ServiceResult.Failure("Invalid device registration.");
        var existing = await _tokenRepo.GetByToken(request.Token);
        if (existing != null)
        {
            // Update: reassign to this user and refresh timestamp
            existing.UserId = userId;
            existing.DeviceName = request.DeviceName;
            existing.AppType = request.AppType;
            existing.LastUsedAt = DateTime.UtcNow;
            _tokenRepo.Update(existing);
        }
        else
        {
            var token = new UserDeviceToken
            {
                UserId = userId,
                Token = request.Token,
                DeviceName = request.DeviceName,
                AppType = request.AppType
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
