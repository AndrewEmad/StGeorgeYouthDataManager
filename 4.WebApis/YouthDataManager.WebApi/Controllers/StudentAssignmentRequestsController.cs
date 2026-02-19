using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentAssignmentRequestsController : ControllerBase
{
    private readonly IStudentAssignmentRequestService _service;

    public StudentAssignmentRequestsController(IStudentAssignmentRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequestDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _service.CreateRequest(dto.StudentId, userId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(new { id = result.Data });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _service.GetMyRequests(userId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetPendingForStudent(Guid studentId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _service.GetPendingForStudent(studentId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        var dto = result.Data;
        if (dto != null && dto.RequestedByUserId != userId)
            return Ok((StudentAssignmentRequestDto?)null);
        return Ok(dto);
    }

    [HttpGet("pending")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetPending()
    {
        var result = await _service.GetPendingForAdmin();
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminUserId))
            return Unauthorized();

        var result = await _service.Approve(id, adminUserId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Reject(Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var adminUserId))
            return Unauthorized();

        var result = await _service.Reject(id, adminUserId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }
}
