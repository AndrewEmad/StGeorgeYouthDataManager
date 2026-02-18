using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public FollowUpController(IFollowUpCommandsService commands, IFollowUpQueriesService queries)
    {
        _commands = commands;
        _queries = queries;
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
}
