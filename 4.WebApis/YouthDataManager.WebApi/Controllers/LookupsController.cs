using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Authorization;

namespace YouthDataManager.WebApi.Controllers;
[PersonalWorkspace]
[ApiController,Route("api/lookups"),Authorize]
public sealed class LookupsController(IApplicationStore store,ResourceAccess access) : ControllerBase
{
    [HttpGet("{kind}")]
    public async Task<IActionResult> Read(string kind,string? search=null,Guid? servantId=null,int page=1,int pageSize=25,CancellationToken ct=default)
    {
        if(page<1 || pageSize is <1 or >100 || (long)(page-1)*pageSize>int.MaxValue || servantId==Guid.Empty)return BadRequest("Invalid lookup query.");
        if(kind=="report-actors")
        {
            var users=store.Query<ApplicationUser>(false).Where(u=>access.Privileged || u.Id==access.UserId);
            if(!string.IsNullOrWhiteSpace(search)){var term=ArabicNormalizer.Normalize(search.Trim());users=users.Where(u=>u.NormalizedFullName.Contains(term));}
            return Ok(new {Items=await users.OrderBy(u=>u.FullName).ThenBy(u=>u.Id).Skip((page-1)*pageSize).Take(pageSize).Select(u=>new {u.Id,Name=u.FullName}).ToListAsync(ct),TotalCount=await users.CountAsync(ct),Page=page,PageSize=pageSize});
        }
        if(kind=="servants")
        {
            var q=EligibleUsers.Query(store);if(!string.IsNullOrWhiteSpace(search)){var term=ArabicNormalizer.Normalize(search.Trim());q=q.Where(u=>u.NormalizedFullName.Contains(term));}
            return Ok(new {Items=await q.OrderBy(u=>u.FullName).ThenBy(u=>u.Id).Skip((page-1)*pageSize).Take(pageSize).Select(u=>new {u.Id,Name=u.FullName}).ToListAsync(ct),TotalCount=await q.CountAsync(ct),Page=page,PageSize=pageSize});
        }
        if(kind is not ("students" or "recipients"))return BadRequest("Unknown lookup.");
        if(!access.Privileged && servantId.HasValue && servantId!=access.UserId)return Forbid();
        var students=store.Query<Student>(false).Where(s=>access.Privileged || s.ServantId==access.UserId);
        if(servantId.HasValue)students=students.Where(s=>s.ServantId==servantId);
        if(!string.IsNullOrWhiteSpace(search)){var term=ArabicNormalizer.Normalize(search.Trim());students=students.Where(s=>s.NormalizedFullName.Contains(term));}
        var ordered=students.OrderBy(s=>s.FullName).ThenBy(s=>s.Id).Skip((page-1)*pageSize).Take(pageSize);
        if(kind=="recipients")return Ok(new {Items=await ordered.Select(s=>new {s.Id,Name=s.FullName,s.Phone,s.SecondaryPhone}).ToListAsync(ct),TotalCount=await students.CountAsync(ct),Page=page,PageSize=pageSize});
        return Ok(new {Items=await ordered.Select(s=>new {s.Id,Name=s.FullName}).ToListAsync(ct),TotalCount=await students.CountAsync(ct),Page=page,PageSize=pageSize});
    }
}
