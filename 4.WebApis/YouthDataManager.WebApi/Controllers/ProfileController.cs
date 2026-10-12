using Microsoft.Extensions.Options;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;
using YouthDataManager.Auth.Service.Abstractions.Commands;
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

[AllowRestrictedAccount]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPhotoUploadService _photos;
    private readonly PhotoUploadOptions _photoOptions;
    private readonly IAuthCommandsService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(IUserService userService, IAuthCommandsService authService, UserManager<ApplicationUser> userManager,IPhotoUploadService photos,IOptions<PhotoUploadOptions> photoOptions)
    {
        _userService = userService;
        _authService = authService;
        _userManager = userManager;_photos=photos;_photoOptions=photoOptions.Value;
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

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return Unauthorized();
        var session = await _authService.Issue(user, Guid.NewGuid());
        return session.Status == ServiceResultStatus.Success ? Ok(session.Data) : BadRequest(session);
    }
    [HttpPost("photo"),RequestSizeLimit(5_242_880),RequestFormLimits(MultipartBodyLengthLimit=5_242_880)]
    public async Task<IActionResult> UploadPhoto(IFormFile? file)
    {
        if(file==null || file.Length==0)return BadRequest(new {message="اختر ملف صورة."});
        var user=await _userManager.GetUserAsync(User);if(user==null)return Unauthorized();
        await using var stream=file.OpenReadStream();var photo=await _photos.ProcessAndSaveAsync(stream,file.ContentType,"servant",user.Id,HttpContext.RequestAborted);
        if(photo.Status!=ServiceResultStatus.Success)return BadRequest(new {message=photo.Message});
        user.PhotoPath=photo.Data;user.UpdatedAt=DateTime.UtcNow;var saved=await _userManager.UpdateAsync(user);
        if(!saved.Succeeded)return StatusCode(500,new {message="تعذر حفظ بيانات الصورة. الصورة السابقة ما زالت محفوظة."});
        return Ok(new {photoPath=user.PhotoPath});
    }
    [HttpGet("photo")]
    public async Task<IActionResult> Photo()
    {
        var user=await _userManager.GetUserAsync(User);if(user==null || string.IsNullOrWhiteSpace(user.PhotoPath))return NotFound();
        var basePath=Path.GetFullPath(_photoOptions.UploadBasePath);var path=Path.GetFullPath(Path.Combine(basePath,user.PhotoPath));
        if(!path.StartsWith(basePath+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path))return NotFound();
        return PhysicalFile(path,"image/jpeg");
    }

}
