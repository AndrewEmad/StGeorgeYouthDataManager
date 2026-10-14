using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
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

[PersonalWorkspace]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentQueriesController : ControllerBase
{
    private readonly ResourceAccess _access;
    private readonly IApplicationStore _store;
    private readonly IStudentQueriesService _service;
    private readonly PhotoUploadOptions _photoOptions;

    public StudentQueriesController(IStudentQueriesService service, ResourceAccess access, IOptions<PhotoUploadOptions> photoOptions,IApplicationStore store)
    {
        _service = service; _access = access; _store=store;
        _photoOptions = photoOptions.Value;
    }

    [HttpGet("paged")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? area = null, [FromQuery] string? academicYear = null, [FromQuery] int? gender = null, [FromQuery] Guid? servantId = null, [FromQuery] bool? hasServant = null, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null, [FromQuery] int? birthMonth = null)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest("Invalid pagination.");
        var filter = new StudentListFilter(search, area, academicYear, gender, servantId, hasServant, sortBy, sortDesc, birthMonth);
        var result = await _service.GetPaged(filter, page, pageSize);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
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
        if (!_access.Privileged && servantId != _access.UserId) return Forbid();
        var result = await _service.GetByServantId(servantId);
        return Ok(result.Data);
    }

    [HttpGet("unassigned-for-servant")]
    public async Task<IActionResult> GetUnassignedForServant([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
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
    public async Task<IActionResult> GetPagedForServant([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, [FromQuery] string? area = null, [FromQuery] string? academicYear = null, [FromQuery] int? gender = null, [FromQuery] string? segment = null)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var servantId))
            return Unauthorized();

        var result = await _service.GetPagedForServant(servantId, page, pageSize, search, area, academicYear, gender, segment);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!await _access.Student(id,historical:true)) return Forbid();
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
    public async Task<IActionResult> GetEditHistory(Guid id)
    {
        if(!await _access.Student(id,historical:true))return Forbid();
        var result = await _service.GetEditHistory(id);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok(result.Data);
    }

    [HttpGet("{id}/audit")]
    public async Task<IActionResult> Audit(Guid id,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if(page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue)return BadRequest("Invalid pagination.");
        if(!await _access.Student(id,ct,historical:true))return Forbid();
        var key=id.ToString();var q=_store.Query<AuditEntry>(false).Where(a=>a.EntityType=="Student" && a.EntityId==key);
        var rows=from a in q join u in _store.Query<ApplicationUser>(false) on a.ActorId equals u.Id into people from u in people.DefaultIfEmpty()
                 select new {a.Id,a.Timestamp,ActorName=u==null ? null : u.FullName,a.Reason,a.BeforeJson,a.AfterJson};
        return Ok(new {Items=await rows.OrderByDescending(a=>a.Timestamp).ThenByDescending(a=>a.Id).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct),TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }

    [HttpGet("{id}/photo")]
    public async Task<IActionResult> GetPhoto(Guid id)
    {
        if (!await _access.Student(id,historical:true)) return Forbid();
        var photoPath=await _store.Query<Student>(false,_access.Privileged).Where(s=>s.Id==id).Select(s=>s.PhotoPath).SingleOrDefaultAsync(HttpContext.RequestAborted);
        if(string.IsNullOrWhiteSpace(photoPath))return NotFound();
        var basePath=Path.GetFullPath(_photoOptions.UploadBasePath);var fullPath=Path.GetFullPath(Path.Combine(basePath,photoPath));
        if(!fullPath.StartsWith(basePath+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))return NotFound();
        return PhysicalFile(fullPath, "image/jpeg");
    }
}
