using YouthDataManager.WebApi.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[PersonalWorkspace]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ResourceAccess _access;
    private readonly IReportQueriesService _service;

    public ReportsController(IReportQueriesService service, ResourceAccess access)
    {
        _service = service; _access = access;
    }

    [HttpGet("dashboard/servant")]
    public async Task<IActionResult> GetServantDashboard()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetServantDashboard(userId);
        return result.Status == ServiceResultStatus.Success ? Ok(result.Data) : StatusCode(500,result);
    }

    [HttpGet("dashboard/manager")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    // [Authorize(Roles = "Manager")] // Add specific role if needed
    public async Task<IActionResult> GetManagerDashboard()
    {
        var result = await _service.GetManagerDashboard();
        return Ok(result.Data);
    }

    [HttpGet("servant-performances-paged")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetServantPerformancesPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetServantPerformancesPaged(page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("servant/{servantId:guid}/performance")]
    public async Task<IActionResult> GetServantPerformance(Guid servantId)
    {
        if (!_access.Privileged && servantId != _access.UserId) return Forbid();
        var result = await _service.GetServantPerformance(servantId);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("servant-stats")]
    public async Task<IActionResult> GetServantStats([FromQuery] Guid[]? ids = null)
    {
        var list = ids ?? Array.Empty<Guid>();
        if (!_access.Privileged && list.Any(id => id != _access.UserId)) return Forbid();
        var result = await _service.GetServantStats(list);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("servants-paged-with-stats")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetServantsPagedWithStats([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? role = null, [FromQuery] bool? isActive = null, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid pagination.");
        var result = await _service.GetServantsPagedWithStats(page, pageSize, search, role, isActive, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("export/students")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> ExportStudents([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportStudentsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students.xlsx");
    }

    [HttpGet("export/calls")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> ExportCalls([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportCallsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Calls.xlsx");
    }

    [HttpGet("export/visits")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> ExportVisits([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportVisitsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Visits.xlsx");
    }

    [HttpGet("servant-activity")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetServantActivitySummary([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] Guid? servantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetServantActivitySummary(dateFrom, dateTo, servantId, page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("students-no-contact")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetStudentsWithNoRecentContact([FromQuery] int days = 14, [FromQuery] Guid? servantId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetStudentsWithNoRecentContact(days, servantId, page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("students-by-area")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetStudentsByArea([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetStudentsByArea(page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("students-by-academic-year")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetStudentsByAcademicYear([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetStudentsByAcademicYear(page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("students-by-birth-month")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetStudentsByBirthMonth([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        var result = await _service.GetStudentsByBirthMonth(page, pageSize, sortBy, sortDesc);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }
}
