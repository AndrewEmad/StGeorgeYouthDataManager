using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentCommandsController : ControllerBase
{
    private const string CsvHeader = "FullName,Phone,BirthDate,Address,Area,College,AcademicYear,ConfessionFather,Gender";
    private static readonly byte[] CsvTemplateBytes = Encoding.UTF8.GetBytes(
        CsvHeader + "\n" +
        "أحمد محمد,01234567890,2000-01-15,,المنطقة أ,كلية هندسة,ثالثة,أب متى,0\n" +
        "مريم سعيد,01112223334,,,المنطقة ب,كلية آداب,ثانية,,1\n");

    private readonly IStudentCommandsService _service;

    public StudentCommandsController(IStudentCommandsService service)
    {
        _service = service;
    }

    [HttpGet("csv-template")]
    public IActionResult GetCsvTemplate()
    {
        return File(CsvTemplateBytes, "text/csv", "students_template.csv");
    }

    [HttpPost("bulk-import")]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> BulkImport([FromBody] List<CreateStudentRequest>? items)
    {
        if (items == null || items.Count == 0)
            return BadRequest(new { message = "No items to import." });
        var created = 0;
        var errors = new List<BulkImportError>();
        for (var i = 0; i < items.Count; i++)
        {
            var req = items[i];
            if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Phone))
            {
                errors.Add(new BulkImportError { Row = i + 1, Message = "FullName and Phone are required." });
                continue;
            }
            var result = await _service.Create(req);
            if (result.Status == ServiceResultStatus.Success)
                created++;
            else
                errors.Add(new BulkImportError { Row = i + 1, Message = result.Message ?? "Create failed." });
        }
        return Ok(new BulkImportResult { Created = created, Errors = errors });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        var result = await _service.Create(request);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return Ok(result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentRequest request)
    {
        if (id != request.Id) return BadRequest("ID mismatch");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var u) ? u : null;
        var isAdmin = User.IsInRole("Admin");

        var result = await _service.Update(request, userId, isAdmin);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.Delete(id);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return NoContent();
    }

    [HttpPost("{id}/assign/{servantId}")]
    public async Task<IActionResult> Assign(Guid id, Guid servantId)
    {
        var result = await _service.AssignToServant(id, servantId);
        
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);

        return Ok();
    }
}

public class BulkImportResult
{
    public int Created { get; set; }
    public List<BulkImportError> Errors { get; set; } = new();
}

public class BulkImportError
{
    public int Row { get; set; }
    public string Message { get; set; } = "";
}
