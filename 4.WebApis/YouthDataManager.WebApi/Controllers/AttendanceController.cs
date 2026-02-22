using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("students")]
    [Authorize(Roles = RolePolicies.CanRecordStudentAttendance)]
    public async Task<IActionResult> RecordStudentAttendance([FromBody] RecordAttendanceRequest request)
    {
        if (request?.Date == null || request.Ids == null || request.Ids.Count == 0)
            return BadRequest("يجب تحديد التاريخ واختيار مخدوم واحد على الأقل.");
        var result = await _attendanceService.RecordStudentAttendanceAsync(request.Date.Value, request.Ids);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result.Message);
        return Ok();
    }

    [HttpPost("servants")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> RecordServantAttendance([FromBody] RecordAttendanceRequest request)
    {
        if (request?.Date == null || request.Ids == null || request.Ids.Count == 0)
            return BadRequest("يجب تحديد التاريخ واختيار خادم واحد على الأقل.");
        var result = await _attendanceService.RecordServantAttendanceAsync(request.Date.Value, request.Ids);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result.Message);
        return Ok();
    }
}

public class RecordAttendanceRequest
{
    public DateTime? Date { get; set; }
    public List<Guid> Ids { get; set; } = new();
}
