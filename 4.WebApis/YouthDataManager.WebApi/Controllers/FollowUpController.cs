using YouthDataManager.WebApi.Authorization;
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
    private readonly ResourceAccess _access;
    private readonly IFollowUpCommandsService _commands;
    private readonly IFollowUpQueriesService _queries;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpController(
        IFollowUpCommandsService commands, ResourceAccess access,
        IFollowUpQueriesService queries,
        UserManager<ApplicationUser> userManager)
    {
        _commands = commands; _access = access;
        _queries = queries;
        _userManager = userManager;
    }

    [HttpPost("call")]
    public async Task<IActionResult> RegisterCall([FromBody] CreateCallLogRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _commands.RegisterCall(request, userId);
        
        if (result.Status != ServiceResultStatus.Success)
            return result.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : result.Errors.Any(e=>e.Code=="Conflict") ? Conflict(result) : BadRequest(result);

        return Ok(result.Data);
    }

    [HttpPost("visit")]
    public async Task<IActionResult> RegisterVisit([FromBody] CreateHomeVisitRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _commands.RegisterVisit(request, userId);
        
        if (result.Status != ServiceResultStatus.Success)
            return result.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : result.Errors.Any(e=>e.Code=="Conflict") ? Conflict(result) : BadRequest(result);

        return Ok(result.Data);
    }

    [HttpPut("visit/{id:guid}")]
    public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateHomeVisitRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _commands.UpdateVisit(id, request, userId);

        if (result.Status != ServiceResultStatus.Success)
            return result.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : result.Errors.Any(e=>e.Code=="Conflict") ? Conflict(result) : BadRequest(result);

        return Ok();
    }

    [HttpPut("call/{id:guid}")]
    public async Task<IActionResult> UpdateCall(Guid id,UpdateCallLogRequest request) => Result(await _commands.UpdateCall(id,request,_access.UserId));
    [HttpPost("call/{id:guid}/void")]
    public async Task<IActionResult> VoidCall(Guid id,VoidActivityRequest request) => Result(await _commands.Void(id,false,request));
    [HttpPost("visit/{id:guid}/void")]
    public async Task<IActionResult> VoidVisit(Guid id,VoidActivityRequest request) => Result(await _commands.Void(id,true,request));
    private IActionResult Result(ServiceResult r) => r.Status==ServiceResultStatus.Success ? Ok() : r.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : r.Errors.Any(e=>e.Code=="Conflict") ? Conflict(r) : BadRequest(r);

    [HttpGet("history/calls/{studentId}")]
    public async Task<IActionResult> GetCallHistory(Guid studentId)
    {
        if (!await _access.Student(studentId)) return Forbid();
        var result = await _queries.GetStudentCallHistory(studentId);
        return result.Status==ServiceResultStatus.Success ? Ok(result.Data) : StatusCode(500,result);
    }

    [HttpGet("history/visits/{studentId}")]
    public async Task<IActionResult> GetVisitHistory(Guid studentId)
    {
        if (!await _access.Student(studentId)) return Forbid();
        var result = await _queries.GetStudentVisitHistory(studentId);
        return result.Status==ServiceResultStatus.Success ? Ok(result.Data) : StatusCode(500,result);
    }

    [HttpGet("history/calls/servant/{servantId}")]
    public async Task<IActionResult> GetServantCallHistory(Guid servantId)
    {
        if (!_access.Privileged && servantId != _access.UserId) return Forbid();
        var result = await _queries.GetServantCallHistory(servantId);
        return result.Status==ServiceResultStatus.Success ? Ok(result.Data) : StatusCode(500,result);
    }

    [HttpGet("history/visits/servant/{servantId}")]
    public async Task<IActionResult> GetServantVisitHistory(Guid servantId)
    {
        if (!_access.Privileged && servantId != _access.UserId) return Forbid();
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
