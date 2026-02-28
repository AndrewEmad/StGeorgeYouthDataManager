using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Notifications.Service.Abstractions;
using YouthDataManager.Notifications.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules()
    {
        var result = await _notificationService.GetSchedules(GetUserId());
        return Ok(result.Data);
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleRequest request)
    {
        var result = await _notificationService.CreateSchedule(GetUserId(), request);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpDelete("schedules/{id}")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var result = await _notificationService.DeleteSchedule(GetUserId(), id);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }

    [HttpPost("device-token")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var result = await _notificationService.RegisterDeviceToken(GetUserId(), request);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }

    [HttpDelete("device-token")]
    public async Task<IActionResult> UnregisterDeviceToken([FromBody] UnregisterDeviceTokenRequest request)
    {
        var result = await _notificationService.UnregisterDeviceToken(GetUserId(), request);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }
}
