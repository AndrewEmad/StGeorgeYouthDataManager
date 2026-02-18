using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Reports.Service.Abstractions.DTOs;
using YouthDataManager.Reports.Service.Abstractions.Queries;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportQueriesService _service;

    public ReportsController(IReportQueriesService service)
    {
        _service = service;
    }

    [HttpGet("dashboard/servant")]
    public async Task<IActionResult> GetServantDashboard()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetServantDashboard(userId);
        return Ok(result.Data);
    }

    [HttpGet("dashboard/manager")]
    // [Authorize(Roles = "Manager")] // Add specific role if needed
    public async Task<IActionResult> GetManagerDashboard()
    {
        var result = await _service.GetManagerDashboard();
        return Ok(result.Data);
    }

    [HttpGet("servant/{servantId:guid}/performance")]
    public async Task<IActionResult> GetServantPerformance(Guid servantId)
    {
        var result = await _service.GetServantPerformance(servantId);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("servant-stats")]
    public async Task<IActionResult> GetServantStats([FromQuery] Guid[]? ids = null)
    {
        var list = ids ?? Array.Empty<Guid>();
        var result = await _service.GetServantStats(list);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("export/students")]
    public async Task<IActionResult> ExportStudents([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportStudentsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students.xlsx");
    }

    [HttpGet("export/calls")]
    public async Task<IActionResult> ExportCalls([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportCallsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Calls.xlsx");
    }

    [HttpGet("export/visits")]
    public async Task<IActionResult> ExportVisits([FromQuery] ReportRequestDto filter)
    {
        var result = await _service.ExportVisitsExcel(filter);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result);
        
        return File(result.Data!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Visits.xlsx");
    }
}
