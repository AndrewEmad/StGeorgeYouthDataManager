using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Students.Service.Abstractions.Queries;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentQueriesController : ControllerBase
{
    private readonly IStudentQueriesService _service;

    public StudentQueriesController(IStudentQueriesService service)
    {
        _service = service;
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] string? area = null, [FromQuery] string? academicYear = null, [FromQuery] int? gender = null, [FromQuery] Guid? servantId = null, [FromQuery] bool? hasServant = null, [FromQuery] string? sortBy = null, [FromQuery] bool? sortDesc = null)
    {
        if (pageSize > 50) pageSize = 50;
        var filter = new StudentListFilter(search, area, academicYear, gender, servantId, hasServant, sortBy, sortDesc);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetById(id);
        if (result.Status != ServiceResultStatus.Success)
            return NotFound(result);
        if (result.Data != null && !User.IsInRole("Admin"))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdClaim, out var userId) && result.Data.ServantId != userId)
                return Forbid();
        }
        return Ok(result.Data);
    }
}
