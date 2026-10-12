using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.WebApi.Controllers;
namespace YouthDataManager.WebApi.Operations;
public static class ImportCandidates
{
    public static async Task<Dictionary<int,ImportCandidate[]>> Find(IApplicationStore store,IReadOnlyList<ImportRowInput> rows,CancellationToken ct)
    {
        var names=rows.Select(r=>ArabicNormalizer.Normalize(r.Student.FullName?.Trim() ?? "")).Where(n=>n!="").Distinct().ToArray();
        var phones=rows.SelectMany(r=>new[]{PhoneFields.Canonical(r.Student.Phone),PhoneFields.Canonical(r.Student.SecondaryPhone)}).Where(n=>n!="").Distinct().ToArray();
        var possible=await store.Query<Student>(false).Where(s=>names.Contains(s.NormalizedFullName) || phones.Contains(s.NormalizedPhone!) || phones.Contains(s.NormalizedSecondaryPhone!))
            .Select(s=>new {s.Id,s.FullName,s.NormalizedFullName,s.NormalizedPhone,s.NormalizedSecondaryPhone}).ToListAsync(ct);
        var entries=rows.Select(r=>new {r.Row,r.Student.FullName,Name=ArabicNormalizer.Normalize(r.Student.FullName?.Trim() ?? ""),Phones=new[]{PhoneFields.Canonical(r.Student.Phone),PhoneFields.Canonical(r.Student.SecondaryPhone)}.Where(k=>k!="").Distinct().ToArray()}).ToArray();
        var fileNames=entries.Where(r=>r.Name!="").GroupBy(r=>r.Name).ToDictionary(g=>g.Key,g=>g.Select(r=>new ImportCandidate(null,r.FullName,r.Row)).ToArray());
        var filePhones=entries.SelectMany(r=>r.Phones.Select(phone=>new {Phone=phone,Candidate=new ImportCandidate(null,r.FullName,r.Row)})).GroupBy(r=>r.Phone).ToDictionary(g=>g.Key,g=>g.Select(r=>r.Candidate).ToArray());
        var savedNames=possible.GroupBy(s=>s.NormalizedFullName).ToDictionary(g=>g.Key,g=>g.Select(s=>new ImportCandidate(s.Id,s.FullName,null)).ToArray());
        var savedPhones=possible.SelectMany(s=>new[]{s.NormalizedPhone,s.NormalizedSecondaryPhone}.Where(k=>k!=null).Distinct().Select(phone=>new {Phone=phone!,Candidate=new ImportCandidate(s.Id,s.FullName,null)})).GroupBy(s=>s.Phone).ToDictionary(g=>g.Key,g=>g.Select(s=>s.Candidate).ToArray());
        return entries.ToDictionary(r=>r.Row,r=>savedNames.GetValueOrDefault(r.Name,[]).Concat(fileNames.GetValueOrDefault(r.Name,[]))
            .Concat(r.Phones.SelectMany(phone=>savedPhones.GetValueOrDefault(phone,[]).Concat(filePhones.GetValueOrDefault(phone,[]))))
            .Where(c=>c.Row!=r.Row).DistinctBy(c=>new {c.StudentId,c.Row}).Take(10).ToArray());
    }
}
public record ImportCandidate(Guid? StudentId,string Name,int? Row);
