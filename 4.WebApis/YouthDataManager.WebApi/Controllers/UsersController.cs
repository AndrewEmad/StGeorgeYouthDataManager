using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions;
using YouthDataManager.Users.Service.Abstractions.DTOs;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _userService.GetAllAsync();
        return Ok(result.Data);
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] string? role = null, [FromQuery] bool? isActive = null)
    {
        if (pageSize > 50) pageSize = 50;
        var result = await _userService.GetPagedAsync(page, pageSize, search, role, isActive);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (result.Data == null) return NotFound(result.Message);
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok();
    }

    [HttpPost("{id}/set-password")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordDto dto)
    {
        if (dto == null) return BadRequest("Invalid request.");
        var result = await _userService.SetPasswordAsync(id, dto);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok();
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _userService.ToggleStatusAsync(id);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        if (result.Status != ServiceResultStatus.Success) return BadRequest(result.Message);
        return NoContent();
    }
}
