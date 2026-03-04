using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Domain.Entities;
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
    private readonly IJwtTokenService _jwtTokenService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(IUserService userService, IJwtTokenService jwtTokenService, UserManager<ApplicationUser> userManager)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _userManager = userManager;
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

        var hadProfileIncomplete = User.FindFirst("profile_incomplete")?.Value == "true";
        if (hadProfileIncomplete)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isExempt = roles.Contains("Admin") || roles.Contains("Priest");
                var profileNowComplete = isExempt || (
                    !string.IsNullOrWhiteSpace(user.FullName) &&
                    !string.IsNullOrWhiteSpace(user.Phone) &&
                    !string.IsNullOrWhiteSpace(user.Address));
                if (profileNowComplete)
                {
                    var newToken = _jwtTokenService.GenerateToken(user, roles, false, false);
                    return Ok(new { token = newToken });
                }
            }
        }
        return Ok();
    }
}
