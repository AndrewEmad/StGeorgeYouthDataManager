using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Students.Service.Abstractions.Queries;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentQueriesController : ControllerBase
{
    private readonly IStudentQueriesService _service;
    private readonly PhotoUploadOptions _photoOptions;

    public StudentQueriesController(IStudentQueriesService service, IOptions<PhotoUploadOptions> photoOptions)
    {
        _service = service;
        _photoOptions = photoOptions.Value;
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] string? area = null, [FromQuery] string? academicYear = null, [FromQuery] int? gender = null, [FromQuery] Guid? servantId = null, [FromQuery] bool? hasServant = null, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null, [FromQuery] int? birthMonth = null)
    {
        if (pageSize > 50) pageSize = 50;
        var filter = new StudentListFilter(search, area, academicYear, gender, servantId, hasServant, sortBy, sortDesc, birthMonth);
        var result = await _service.GetPaged(filter, page, pageSize);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result.Data);
    }

    [HttpGet("areas")]
    public async Task<IActionResult> GetDistinctAreas()
    {
        var result = await _service.GetDistinctAreas();
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("academic-years")]
    public async Task<IActionResult> GetDistinctAcademicYears()
    {
        var result = await _service.GetDistinctAcademicYears();
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("servant/{servantId}")]
    public async Task<IActionResult> GetByServant(Guid servantId)
    {
        var result = await _service.GetByServantId(servantId);
        return Ok(result.Data);
    }

    [HttpGet("unassigned-for-servant")]
    public async Task<IActionResult> GetUnassignedForServant([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var servantId))
            return Unauthorized();

        var result = await _service.GetUnassignedForServant(servantId, page, pageSize);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("students-for-servant")]
    public async Task<IActionResult> GetPagedForServant([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? area = null, [FromQuery] string? academicYear = null, [FromQuery] int? gender = null)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var servantId))
            return Unauthorized();

        var result = await _service.GetPagedForServant(servantId, page, pageSize, search, area, academicYear, gender);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        if (result.Status != ServiceResultStatus.Success)
            return NotFound(result);
        if (result.Data != null && !User.IsAdminOrManagerOrPriest())
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var userId) && result.Data.ServantId != userId)
                return Forbid();
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}/edit-history")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetEditHistory(Guid id)
    {
        var result = await _service.GetEditHistory(id);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetPhoto(Guid id)
    {
        var result = await _service.GetById(id);
        if (result.Status != ServiceResultStatus.Success || result.Data == null)
            return NotFound();
        var data = result.Data;
        if (!User.IsAdminOrManagerOrPriest())
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId) || data.ServantId != userId)
                return Forbid();
        }
        if (string.IsNullOrEmpty(data.PhotoPath)) return NotFound();
        var fullPath = Path.Combine(_photoOptions.UploadBasePath, data.PhotoPath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();
        return PhysicalFile(fullPath, "image/jpeg");
    }
}
