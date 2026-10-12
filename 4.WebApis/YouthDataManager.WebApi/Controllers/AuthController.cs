using YouthDataManager.WebApi.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.WebApi.Controllers;

[AllowRestrictedAccount]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthCommandsService _authService;

    public AuthController(IAuthCommandsService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.Login(request);
        if (result.Status != ServiceResultStatus.Success)
            return Unauthorized(result);
        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshToken(request);
        if (result.Status != ServiceResultStatus.Success)
            return Unauthorized(result);
        return Ok(result.Data);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var id = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var userId) ? userId : Guid.Empty;
        var result = await _authService.Logout(id, request.RefreshToken,request.DeviceToken);
        return result.Status == ServiceResultStatus.Success ? Ok() : BadRequest(result);
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

public record LogoutRequest(string RefreshToken,string? DeviceToken=null);
