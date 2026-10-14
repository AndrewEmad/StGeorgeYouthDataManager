using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class AcademicYearNormalizationTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("اولى","سنة اولى")]
    [InlineData("أولي","سنة اولى")]
    [InlineData(" سنة أولي ","سنة اولى")]
    [InlineData("السنة الأُولَى","سنة اولى")]
    [InlineData("سنة ثانية","سنة ثانية")]
    [InlineData("تانية","سنة ثانية")]
    [InlineData("سنه ٣","سنة ثالثة")]
    [InlineData("الفرقة الرابعة بالجامعة","سنة رابعة")]
    [InlineData("خامسه","سنة خامسة")]
    [InlineData("۶","سنة سادسة")]
    [InlineData("سنة سابعه","سنة سابعة")]
    [InlineData("خريج حديث","حديث التخرج")]
    [InlineData("حديثة التخرج","حديث التخرج")]
    [InlineData("خريج ","حديث التخرج")]
    [InlineData("خريجين","حديث التخرج")]
    [InlineData("الخريجين","حديث التخرج")]
    [InlineData("خريجة","حديث التخرج")]
    [InlineData("اتخرجت 2025","حديث التخرج")]
    [InlineData("حديثي التخرج","حديث التخرج")]
    [InlineData("غير معروف","غير معروف")]
    [InlineData("سنة رابعه  ( هيتخرج ٢٠٢٧","سنة رابعة")]
    [InlineData("سنة رابعة (ثانوي)",null)]
    [InlineData("أولى ثانوي",null)]
    [InlineData("سنة ثامنة",null)]
    public void Known_aliases_and_graduate_labels_are_canonical_and_unresolved_stages_are_not_guessed(string input,string? expected) => Assert.Equal(expected,AcademicYears.Canonical(input));

    private static string Script()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null){var path=Path.Combine(directory.FullName,"docs","sql","normalize-student-academic-years.sql");if(File.Exists(path))return File.ReadAllText(path);directory=directory.Parent;}
        throw new FileNotFoundException("Normalization script missing.");
    }
    [Fact]
    public async Task SQL_preview_apply_and_retry_preserve_unknowns_archived_history_and_unrelated_fields()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inputs=new[]{"اولى","أولي","سنة اولي","الفرقة الرابعة بالجامعة","خريج حديث","أولى ثانوي","خريج","سنة ثامنة","", "سنة ثانية","خريجين","خريجة","اتخرجت 2025","حديثي التخرج","غير معروف","سنة رابعه  ( هيتخرج ٢٠٢٧"};
        var students=inputs.Select((year,i)=>new Student {Id=Guid.NewGuid(),FullName="Normalize "+i,NormalizedFullName="normalize "+i,Phone="01012345678",AcademicYear=year,Notes="Keep notes",IsDeleted=i==2}).ToList();db.AddRange(students);await db.SaveChangesAsync();
        await using var connection=new SqlConnection(fixture.Connection);await connection.OpenAsync();
        async Task Run(bool apply){await using var command=new SqlCommand(apply ? Script().Replace("DECLARE @Apply bit = 0;","DECLARE @Apply bit = 1;") : Script(),connection){CommandTimeout=60};await command.ExecuteNonQueryAsync();}
        await Run(false);db.ChangeTracker.Clear();foreach(var row in students)Assert.Equal(row.AcademicYear,(await db.Students.IgnoreQueryFilters().SingleAsync(s=>s.Id==row.Id)).AcademicYear);
        await Run(true);db.ChangeTracker.Clear();
        foreach(var row in students){var saved=await db.Students.IgnoreQueryFilters().SingleAsync(s=>s.Id==row.Id);Assert.Equal(AcademicYears.Canonical(row.AcademicYear) ?? row.AcademicYear,saved.AcademicYear);Assert.Equal(row.IsDeleted,saved.IsDeleted);Assert.Equal("Keep notes",saved.Notes);}
        var changed=students.Count(s=>AcademicYears.Canonical(s.AcademicYear) is { } normalized && normalized!=s.AcademicYear);
        Assert.Equal(changed,await db.StudentEditLogs.IgnoreQueryFilters().CountAsync(e=>students.Select(s=>s.Id).Contains(e.StudentId)));
        var ids=students.Select(s=>s.Id.ToString()).ToArray();Assert.Equal(changed,await db.Set<AuditEntry>().CountAsync(a=>ids.Contains(a.EntityId) && a.Reason=="توحيد السنوات الدراسية"));
        await Run(true);Assert.Equal(changed,await db.StudentEditLogs.IgnoreQueryFilters().CountAsync(e=>students.Select(s=>s.Id).Contains(e.StudentId)));
    }
    [Fact]
    public async Task API_creation_and_import_accept_aliases_as_canonical_and_reject_new_unknown_values()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=actor.UserName,password="password123"});login.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        var create=await client.PostAsJsonAsync("/api/StudentCommands",new {fullName="Canonical API",phone="01012345678",gender=0,academicYear="سنة أولي"});create.EnsureSuccessStatusCode();var id=await create.Content.ReadFromJsonAsync<Guid>();Assert.Equal("سنة اولى",(await db.Students.SingleAsync(s=>s.Id==id)).AcademicYear);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/StudentCommands",new {fullName="Invalid stage",phone="01012345678",gender=0,academicYear="مرحلة مجهولة"})).StatusCode);
        var addition=await client.PostAsJsonAsync("/api/StudentAdditionRequests",new {fullName="Canonical request",phone="01012345678",gender=0,academicYear="خريجة"});addition.EnsureSuccessStatusCode();Assert.Equal("حديث التخرج",(await db.StudentAdditionRequests.SingleAsync(r=>r.FullName=="Canonical request")).AcademicYear);
        var import=await client.PostAsJsonAsync("/api/StudentCommands/bulk-import-v2",new {operationId=Guid.NewGuid(),rows=new[]{new {row=2,duplicateReviewed=true,student=new {fullName="Canonical import",phone="01012345678",gender=0,academicYear="اتخرجت 2025"}}}});import.EnsureSuccessStatusCode();Assert.Equal("حديث التخرج",(await db.Students.SingleAsync(s=>s.FullName=="Canonical import")).AcademicYear);
        // Existing unknown data does not prevent a focused edit of another field.
        var old=new Student {Id=Guid.NewGuid(),FullName="Old unknown",NormalizedFullName="old unknown",Phone="01012345678",AcademicYear="مرحلة مجهولة"};db.Add(old);await db.SaveChangesAsync();
        (await client.PutAsJsonAsync($"/api/StudentCommands/{old.Id}",new {id=old.Id,notes="Other field updated"})).EnsureSuccessStatusCode();db.ChangeTracker.Clear();Assert.Equal("مرحلة مجهولة",(await db.Students.SingleAsync(s=>s.Id==old.Id)).AcademicYear);
    }
}
