using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.Commands;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;

[ApiController,Route("api/StudentCommands"),Authorize(Roles=RolePolicies.AdminManagerPriestRoles)]
public sealed class StudentOperationsController(IApplicationStore store,IStudentCommandsService commands,ResourceAccess access,ICallerContext caller) : ControllerBase
{
    [HttpPost("import-preview"),RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Preview(ImportBatch batch,CancellationToken ct)
    {
        if(batch.Rows.Count is <1 or >5000 || batch.Rows.Any(r=>r.Row<1) || batch.Rows.Select(r=>r.Row).Distinct().Count()!=batch.Rows.Count)return BadRequest("Invalid preview.");
        var candidates=await Operations.ImportCandidates.Find(store,batch.Rows,ct);
        return Ok(batch.Rows.Select(r=>new {r.Row,Candidates=candidates[r.Row],Error=StudentFields.Validate(r.Student.FullName,r.Student.Phone,r.Student.SecondaryPhone,r.Student.Address,r.Student.Area,r.Student.College,r.Student.AcademicYear,r.Student.ConfessionFather,r.Student.Notes,(YouthDataManager.Domain.Enums.Gender)r.Student.Gender,r.Student.BirthDate,r.Student.BirthMonth)}));
    }
    [HttpPost("bulk-import-v2"),RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Import(ImportBatch batch,CancellationToken ct)
    {
        if (batch.OperationId==Guid.Empty || batch.Rows.Count is <1 or >5000 || batch.Rows.Any(r=>r.Row<1) || batch.Rows.Select(r=>r.Row).Distinct().Count()!=batch.Rows.Count) return BadRequest("Invalid import operation or original row references.");
        var outcomes=new List<ImportRowResult>();
        var candidates=await Operations.ImportCandidates.Find(store,batch.Rows,ct);
        foreach (var row in batch.Rows)
        {
            ct.ThrowIfCancellationRequested();
            var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(row.Student))));
            var prior=await store.Query<ImportRowOutcome>().SingleOrDefaultAsync(r=>r.OperationId==batch.OperationId && r.ActorId==access.UserId && r.Row==row.Row,ct);
            if (prior?.StudentId!=null)
            {
                outcomes.Add(new(row.Row,prior.StudentId,prior.PayloadHash==hash ? null : "This row was already imported with different values.",true)); continue;
            }
            if (prior!=null) { store.Remove(prior);await store.Save(ct); }
            ServiceResult<Guid> result;
            try { result=candidates[row.Row].Length>0 && !row.DuplicateReviewed ? ServiceResult<Guid>.Failure("Review matching names or shared phones before importing this row.") : await commands.Create(row.Student); }
            catch (DbUpdateException) { result=ServiceResult<Guid>.Failure("Row could not be persisted. Check the submitted references and limits."); }
            var outcome=new ImportRowOutcome {OperationId=batch.OperationId,ActorId=access.UserId,Row=row.Row,PayloadHash=hash,StudentId=result.Status==ServiceResultStatus.Success ? result.Data : null,Error=result.Message};
            store.Add(outcome);await store.Save(ct);
            outcomes.Add(new(row.Row,outcome.StudentId,outcome.Error,false));
        }
        return Ok(new ImportBatchResult(batch.OperationId,outcomes));
    }
    [HttpGet("import/{operationId:guid}")]
    public async Task<IActionResult> ImportStatus(Guid operationId,CancellationToken ct) => Ok(await store.Query<ImportRowOutcome>(false).Where(r=>r.OperationId==operationId && r.ActorId==access.UserId).OrderBy(r=>r.Row).Select(r=>new {r.Row,r.StudentId,r.Error}).ToListAsync(ct));

    [HttpPost("bulk-assignment")]
    public async Task<IActionResult> Assign(BulkAssignment batch,CancellationToken ct)
    {
        if (batch.Ids.Count is <1 or >5000) return BadRequest("Invalid selection.");
        var outcomes=new List<object>();
        foreach (var id in batch.Ids.Distinct())
        {
            ct.ThrowIfCancellationRequested();
            var result=batch.ServantId.HasValue ? await commands.AssignToServant(id,batch.ServantId.Value) : await commands.UnassignFromServant(id);
            outcomes.Add(new {StudentId=id,Success=result.Status==ServiceResultStatus.Success,Error=result.Message});
        }
        return Ok(new {Outcomes=outcomes});
    }
    [HttpPost("{id:guid}/unassign")]
    public async Task<IActionResult> Unassign(Guid id)
    {
        var r=await commands.UnassignFromServant(id);return r.Status==ServiceResultStatus.Success ? Ok() : BadRequest(r);
    }
    [HttpPost("bulk-area")]
    public async Task<IActionResult> Area([FromBody] BulkAreaRequest batch)
    {
        var result=await commands.UpdateAreas(batch);
        return result.Status==ServiceResultStatus.Success ? Ok(result.Data) : result.Errors.Any(e=>e.Code=="Forbidden") ? Forbid() : BadRequest(result);
    }
    [HttpPost("bulk-academic-year")]
    public async Task<IActionResult> AcademicYear([FromBody] BulkAcademicYearRequest batch)
    {
        var result = await commands.UpdateAcademicYears(batch);
        if (result.Status == ServiceResultStatus.Success) return Ok(result.Data);
        return result.Errors.Any(e => e.Code == "Forbidden") ? Forbid() : BadRequest(result);
    }
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id,CancellationToken ct)
    {
        var student=await store.Query<Student>(true,true).SingleOrDefaultAsync(s=>s.Id==id,ct);
        if (student==null) return NotFound();
        if (!student.IsDeleted) return Ok(new {Restored=false,AlreadyActive=true});
        if(string.IsNullOrWhiteSpace(caller.Reason) || caller.Reason.Length>1000)return BadRequest("A restoration reason is required (1–1000 characters).");
        if(caller.ExpectedVersion!=null && caller.ExpectedVersion!=Convert.ToBase64String(student.Version))return Conflict("Student changed. Review before restoring.");
        student.IsDeleted=false;student.ServantId=null;student.UpdatedAt=caller.UtcNow;await store.Save(ct);
        return Ok(new {Restored=true,AlreadyActive=false});
    }
    [HttpGet("archived")]
    public async Task<IActionResult> Archived(int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if (page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue) return BadRequest("Invalid pagination.");
        var q=store.Query<Student>(false,true).Where(s=>s.IsDeleted);
        return Ok(new {Items=await q.OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Skip((page-1)*pageSize).Take(pageSize).Select(s=>new {s.Id,s.FullName,s.Area,s.UpdatedAt,s.Version}).ToListAsync(ct),TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
    }
}
public record ImportBatch(Guid OperationId,List<ImportRowInput> Rows);
public record ImportRowInput(int Row,CreateStudentRequest Student,bool DuplicateReviewed=false);
public record BulkAssignment(List<Guid> Ids,Guid? ServantId);

public record ImportRowResult(int Row,Guid? StudentId,string? Error,bool Replayed);
public record ImportBatchResult(Guid OperationId,List<ImportRowResult> Outcomes);
