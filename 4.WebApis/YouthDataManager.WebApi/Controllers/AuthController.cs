using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthCommandsService _authService;

    public AuthController(IAuthCommandsService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.Login(request);
        if (result.Status != ServiceResultStatus.Success)
            return Unauthorized(result);
        return Ok(result.Data);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshToken(request);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        var result = await _authService.ChangePassword(userId, request);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }
}
