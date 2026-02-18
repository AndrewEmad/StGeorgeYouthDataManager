using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentRemovalRequestsController : ControllerBase
{
    private readonly IStudentRemovalRequestService _service;

    public StudentRemovalRequestsController(IStudentRemovalRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRemovalRequestDto dto)
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

        var result = await _service.GetPendingForStudentAndUser(studentId, userId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _service.GetPendingForAdmin();
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
