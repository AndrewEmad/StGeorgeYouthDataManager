using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Domain.Entities;
using YouthDataManager.FollowUp.Service.Abstractions.Commands;
using YouthDataManager.FollowUp.Service.Abstractions.DTOs;
using YouthDataManager.FollowUp.Service.Abstractions.Queries;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FollowUpController : ControllerBase
{
    private readonly IFollowUpCommandsService _commands;
    private readonly IFollowUpQueriesService _queries;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpController(
        IFollowUpCommandsService commands,
        IFollowUpQueriesService queries,
        UserManager<ApplicationUser> userManager)
    {
        _commands = commands;
        _queries = queries;
        _userManager = userManager;
    }

    [HttpPost("call")]
    public async Task<IActionResult> RegisterCall([FromBody] CreateCallLogRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _commands.RegisterCall(request, userId);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return Ok(result.Data);
    }

    [HttpPost("visit")]
    public async Task<IActionResult> RegisterVisit([FromBody] CreateHomeVisitRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _commands.RegisterVisit(request, userId);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return Ok(result.Data);
    }

    [HttpGet("history/calls/{studentId}")]
    public async Task<IActionResult> GetCallHistory(Guid studentId)
    {
        var result = await _queries.GetStudentCallHistory(studentId);
        return Ok(result.Data);
    }

    [HttpGet("history/visits/{studentId}")]
    public async Task<IActionResult> GetVisitHistory(Guid studentId)
    {
        var result = await _queries.GetStudentVisitHistory(studentId);
        return Ok(result.Data);
    }

    [HttpGet("history/calls/servant/{servantId}")]
    public async Task<IActionResult> GetServantCallHistory(Guid servantId)
    {
        var result = await _queries.GetServantCallHistory(servantId);
        return Ok(result.Data);
    }

    [HttpGet("history/visits/servant/{servantId}")]
    public async Task<IActionResult> GetServantVisitHistory(Guid servantId)
    {
        var result = await _queries.GetServantVisitHistory(servantId);
        return Ok(result.Data);
    }

    /// <summary>
    /// Returns active servants (id, fullName) for the visit participant picker, excluding the current user.
    /// </summary>
    [HttpGet("servants")]
    public async Task<IActionResult> GetServantsForVisitPicker()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var servants = await _userManager.GetUsersInRoleAsync("Servant");
        var managers = await _userManager.GetUsersInRoleAsync("Manager");
        var secretaries = await _userManager.GetUsersInRoleAsync("Secretary");
        var all = servants.Concat(managers).Concat(secretaries)
            .GroupBy(u => u.Id).Select(g => g.First())
            .Where(u => u.IsActive && u.Id.ToString() != currentUserId)
            .OrderBy(u => u.FullName ?? u.UserName ?? "")
            .Select(u => new { id = u.Id, fullName = u.FullName ?? u.UserName ?? "" })
            .ToList();
        return Ok(all);
    }
}
