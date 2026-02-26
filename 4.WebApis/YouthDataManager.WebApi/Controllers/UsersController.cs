using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YouthDataManager.Photo.Service.Abstractions;
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
    private readonly IPhotoUploadService _photoUploadService;
    private readonly PhotoUploadOptions _photoOptions;
    private readonly UserManager<YouthDataManager.Domain.Entities.ApplicationUser> _userManager;

    public UsersController(
        IUserService userService,
        IPhotoUploadService photoUploadService,
        IOptions<PhotoUploadOptions> photoOptions,
        UserManager<YouthDataManager.Domain.Entities.ApplicationUser> userManager)
    {
        _userService = userService;
        _photoUploadService = photoUploadService;
        _photoOptions = photoOptions.Value;
        _userManager = userManager;
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

    [HttpPost("{id}/photo")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "لم يتم اختيار ملف." });
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        var result = await _photoUploadService.ProcessAndSaveAsync(file.OpenReadStream(), file.ContentType ?? "", "servant", id);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(new { message = result.Message });
        user.PhotoPath = result.Data;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return Ok(new { photoPath = result.Data });
    }

    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetPhoto(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null || string.IsNullOrEmpty(user.PhotoPath)) return NotFound();
        var fullPath = Path.Combine(_photoOptions.UploadBasePath, user.PhotoPath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        return PhysicalFile(fullPath, "image/jpeg");
    }
}
