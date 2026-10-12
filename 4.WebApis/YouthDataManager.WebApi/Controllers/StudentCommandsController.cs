using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YouthDataManager.Domain.Repositories.Tracking;
using YouthDataManager.Photo.Service.Abstractions;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentCommandsController : ControllerBase
{
    private static readonly byte[] CsvTemplateBytes=Encoding.UTF8.GetBytes("\ufeff\"FullName\",\"Phone\",\"SecondaryPhone\",\"BirthDate\",\"BirthMonth\",\"Address\",\"Area\",\"College\",\"AcademicYear\",\"ConfessionFather\",\"Notes\",\"Gender\"\r\n");
    private readonly IApplicationStore _store;private readonly ICallerContext _caller;
    private readonly ResourceAccess _access;
    private readonly IStudentCommandsService _service;
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPhotoUploadService _photoUploadService;

    public StudentCommandsController(
        IStudentCommandsService service, ResourceAccess access,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IPhotoUploadService photoUploadService,IApplicationStore store,ICallerContext caller)
    {
        _service = service; _access = access;_store=store;_caller=caller;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _photoUploadService = photoUploadService;
    }

    [HttpGet("csv-template")]
    public IActionResult GetCsvTemplate()
    {
        return File(CsvTemplateBytes, "text/csv", "students_template.csv");
    }

    [HttpPost("bulk-import")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> BulkImport([FromBody] List<CreateStudentRequest>? items)
    {
        if(items==null || items.Count is <1 or >5000)return BadRequest("Import requires 1-5000 rows.");
        // Legacy clients use a content-derived operation ID; modern clients retain an explicit ID.
        var hash=System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(items)));
        var operation=Guid.TryParse(Request.Headers["X-Import-Operation"],out var supplied) ? supplied : new Guid(hash.AsSpan(0,16));
        var adapter=new StudentOperationsController(_store,_service,_access,_caller){ControllerContext=ControllerContext};
        var result=await adapter.Import(new ImportBatch(operation,items.Select((student,index)=>new ImportRowInput(index+1,student)).ToList()),HttpContext.RequestAborted);
        if(result is not OkObjectResult {Value:ImportBatchResult batch})return result;
        return Ok(new {Created=batch.Outcomes.Count(r=>r.StudentId!=null && r.Error==null),Errors=batch.Outcomes.Where(r=>r.Error!=null).Select(r=>new BulkImportError {Row=r.Row,Message=r.Error!}),batch.OperationId,batch.Outcomes});
    }

    [HttpPost]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
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
        if (!await _access.Student(id)) return Forbid();
        if (id != request.Id) return BadRequest("ID mismatch");

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? userId = Guid.TryParse(userIdClaim, out var u) ? u : null;
        var isAdmin = User.IsAdminOrManagerOrPriest();

        var result = await _service.Update(request, userId, isAdmin);
        
        if (result.Status != ServiceResultStatus.Success)
            return result.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : result.Errors.Any(e=>e.Code=="Conflict") ? Conflict(result) : BadRequest(result);

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.Delete(id);
        if(result.Status!=ServiceResultStatus.Success){if(result.Errors?.Any(e=>e.Code=="Conflict")==true)return Conflict(result);if(result.Errors?.Any(e=>e.Code=="NotFound")==true)return NotFound(result);if(result.Errors?.Any(e=>e.Code=="Forbidden")==true)return Forbid();return BadRequest(result);}

        return NoContent();
    }

    [HttpPost("{id}/assign/{servantId}")]
    [Authorize(Roles = RolePolicies.AdminManagerPriestRoles)]
    public async Task<IActionResult> Assign(Guid id, Guid servantId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();
        if (!User.IsAdminOrManagerOrPriest() && servantId != userId)
            return Forbid();
        var result = await _service.AssignToServant(id, servantId);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(result);
        return Ok();
    }

    [HttpPost("{id}/photo")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile? file)
    {
        if (!await _access.Student(id)) return Forbid();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "لم يتم اختيار ملف." });
        var student = await _studentRepository.GetById(id);
        if (student == null) return NotFound();
        var result = await _photoUploadService.ProcessAndSaveAsync(file.OpenReadStream(), file.ContentType ?? "", "student", id);
        if (result.Status != ServiceResultStatus.Success)
            return BadRequest(new { message = result.Message });
        student.PhotoPath = result.Data;
        student.UpdatedAt = DateTime.UtcNow;
        _studentRepository.Update(student);
        if(await _unitOfWork.SaveChangesAsync(HttpContext.RequestAborted)<=0)return StatusCode(500,new {message="تعذر حفظ الصورة. بيانات الطالب وصورته السابقة محفوظة؛ أعد محاولة رفع الصورة."});
        return Ok(new { photoPath = result.Data });
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
