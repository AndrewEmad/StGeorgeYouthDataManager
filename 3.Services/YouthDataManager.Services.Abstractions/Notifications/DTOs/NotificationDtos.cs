namespace YouthDataManager.Notifications.Service.Abstractions.DTOs;

public record CreateScheduleRequest(string DaysOfWeek, string TimeOfDay);

public record ScheduleDto(Guid Id, string DaysOfWeek, string TimeOfDay, bool IsActive);

public record RegisterDeviceTokenRequest(string Token, string? DeviceName, string AppType = "Servant");

public record UnregisterDeviceTokenRequest(string Token);
