namespace YouthDataManager.Notifications.Service.Abstractions.DTOs;

public record CreateScheduleRequest(string DaysOfWeek, string TimeOfDay);

public record ScheduleDto(Guid Id, string DaysOfWeek, string TimeOfDay, bool IsActive);

public record RegisterDeviceTokenRequest(string Token, string? DeviceName);

public record UnregisterDeviceTokenRequest(string Token);
