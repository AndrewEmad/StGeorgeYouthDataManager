using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions.DTOs;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        var result = await _userService.GetByIdAsync(userId);
        if (result.Data == null) return NotFound(result.Message);
        return Ok(result.Data);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMyProfileDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        var result = await _userService.UpdateMyProfileAsync(userId, dto);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok();
    }
}
