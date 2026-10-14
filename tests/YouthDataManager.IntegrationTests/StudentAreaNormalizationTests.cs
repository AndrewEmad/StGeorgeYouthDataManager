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
public sealed class StudentAreaNormalizationTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("إبراهيم بك","ابراهيم بك")]
    [InlineData("أرض نوبار","ارض نوبار")]
    [InlineData("الإسكان الصناعى","الاسكان الصناعي")]
    [InlineData("المرج الجديده","المرج الجديدة")]
    [InlineData(" المعادي   الجديده ","المعادى الجديدة")]
    [InlineData("النزهة الجديدة","النزهه الجديده")]
    [InlineData("عزبة رستم","عزبه رستم")]
    [InlineData("كوبري عرابي","كوبرى عرابى")]
    [InlineData("أُم بيومي","ام بيومي")]
    [InlineData("أخرى","اخرى")]
    [InlineData("شبرا البلد","شبرا البلد")]
    [InlineData("شبرا مصر","شبرا مصر")]
    [InlineData("شبرا المحطة","شبرا البلد")]
    [InlineData("شبرا الخيمة","شبرا البلد")]
    [InlineData("شبرا الخيمه","شبرا البلد")]
    [InlineData("ارض نوبار شبرا الخيمه","ارض نوبار")]
    [InlineData("المساكن","الوحدة")]
    [InlineData("الملف شبرا الخيمه","الملف")]
    [InlineData("الوحدة عند المصانع","الوحدة")]
    [InlineData("خلف الكنيسة","شبرا البلد")]
    [InlineData("شارع زكريا","شبرا البلد")]
    [InlineData("منية السيرج","اخرى")]
    [InlineData("ترعة زكريا","اخرى")]
    [InlineData("منطقة قديمة","اخرى")]
    [InlineData("غير معروف","اخرى")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void Approved_spellings_are_canonical_without_merging_distinct_areas(string? input,string expected)=>Assert.Equal(expected,StudentAreas.Canonical(input));
    [Fact]
    public void All_approved_areas_are_unique_and_exactly_canonical(){Assert.Equal(40,StudentAreas.Values.Count);Assert.Equal(40,StudentAreas.Values.Distinct().Count());foreach(var value in StudentAreas.Values)Assert.Equal(value,StudentAreas.Canonical(value));}

    private static string Script()
    {
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null){var path=Path.Combine(directory.FullName,"docs","sql","normalize-student-areas.sql");if(File.Exists(path))return File.ReadAllText(path);directory=directory.Parent;}
        throw new FileNotFoundException("Normalization script missing.");
    }
    [Fact]
    public async Task SQL_preview_apply_and_retry_preserve_unknowns_archived_history_and_unrelated_fields()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inputs=StudentAreas.Values.Concat(new[]{"إبراهيم بك","أرض نوبار","الإسكان الصناعى","المرج الجديده"," المعادي   الجديده ","النزهة الجديدة","عزبة رستم","كوبري عرابي","أُم بيومي","أخرى","شبرا المحطة","شبرا الخيمة","شبرا الخيمه","ارض نوبار شبرا الخيمه","المساكن","الملف شبرا الخيمه","الوحدة عند المصانع","خلف الكنيسة","شارع زكريا","منية السيرج","ترعة زكريا","منطقة قديمة","غير معروف","", "  "}).ToArray();
        var students=inputs.Select((area,i)=>new Student {Id=Guid.NewGuid(),FullName="Normalize "+i,NormalizedFullName="normalize "+i,Phone="012900"+i.ToString("D5"),Area=area,Notes="Keep notes",AcademicYear="سنة رابعة",IsDeleted=i==42 || area=="منطقة قديمة"}).ToList();db.AddRange(students);await db.SaveChangesAsync();
        await using var connection=new SqlConnection(fixture.Connection);await connection.OpenAsync();
        async Task Run(bool apply){await using var command=new SqlCommand(apply ? Script().Replace("DECLARE @Apply bit = 0;","DECLARE @Apply bit = 1;") : Script(),connection){CommandTimeout=60};await command.ExecuteNonQueryAsync();}
        await Run(false);db.ChangeTracker.Clear();foreach(var row in students)Assert.Equal(row.Area,(await db.Students.IgnoreQueryFilters().SingleAsync(s=>s.Id==row.Id)).Area);
        await Run(true);db.ChangeTracker.Clear();
        foreach(var row in students){var saved=await db.Students.IgnoreQueryFilters().SingleAsync(s=>s.Id==row.Id);Assert.Equal(string.IsNullOrWhiteSpace(row.Area) ? row.Area : StudentAreas.Canonical(row.Area),saved.Area);Assert.Equal(row.IsDeleted,saved.IsDeleted);Assert.Equal("Keep notes",saved.Notes);Assert.Equal("سنة رابعة",saved.AcademicYear);}
        var changed=students.Count(s=>StudentAreas.Canonical(s.Area) is { } normalized && !string.IsNullOrWhiteSpace(s.Area) && normalized!=s.Area);
        Assert.Equal(changed,await db.StudentEditLogs.IgnoreQueryFilters().CountAsync(e=>students.Select(s=>s.Id).Contains(e.StudentId)));
        var ids=students.Select(s=>s.Id.ToString()).ToArray();Assert.Equal(changed,await db.Set<AuditEntry>().CountAsync(a=>ids.Contains(a.EntityId) && a.Reason=="توحيد المناطق"));
        await Run(true);Assert.Equal(changed,await db.StudentEditLogs.IgnoreQueryFilters().CountAsync(e=>students.Select(s=>s.Id).Contains(e.StudentId)));
    }
    [Fact]
    public async Task API_creation_and_import_accept_aliases_as_canonical_and_map_unknown_values_to_other()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=actor.UserName,password="password123"});login.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        var create=await client.PostAsJsonAsync("/api/StudentCommands",new {fullName="Canonical area API",phone="01012345678",gender=0,area="شبرا الخيمة"});create.EnsureSuccessStatusCode();var id=await create.Content.ReadFromJsonAsync<Guid>();Assert.Equal("شبرا البلد",(await db.Students.SingleAsync(s=>s.Id==id)).Area);
        (await client.PutAsJsonAsync($"/api/StudentCommands/{id}",new {id,area="ارض نوبار شبرا الخيمه"})).EnsureSuccessStatusCode();db.ChangeTracker.Clear();Assert.Equal("ارض نوبار",(await db.Students.SingleAsync(s=>s.Id==id)).Area);
        var fallback=await client.PostAsJsonAsync("/api/StudentCommands",new {fullName="Other area",phone="01012345678",gender=0,area="منطقة مجهولة"});fallback.EnsureSuccessStatusCode();Assert.Equal("اخرى",(await db.Students.SingleAsync(s=>s.FullName=="Other area")).Area);
        var addition=await client.PostAsJsonAsync("/api/StudentAdditionRequests",new {fullName="Canonical area request",phone="01012345678",gender=0,area="منطقة طلب غير مدرجة"});addition.EnsureSuccessStatusCode();Assert.Equal("اخرى",(await db.StudentAdditionRequests.SingleAsync(r=>r.FullName=="Canonical area request")).Area);
        var import=await client.PostAsJsonAsync("/api/StudentCommands/bulk-import-v2",new {operationId=Guid.NewGuid(),rows=new[]{new {row=2,duplicateReviewed=true,student=new {fullName="Canonical area import",phone="01012345678",gender=0,area="منطقة استيراد غير مدرجة"}}}});import.EnsureSuccessStatusCode();Assert.Equal("اخرى",(await db.Students.SingleAsync(s=>s.FullName=="Canonical area import")).Area);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync($"/api/StudentCommands/{id}",new {id,area=new string('x',101)})).StatusCode);
        // Existing unknown data does not prevent a focused edit of another field.
        var old=new Student {Id=Guid.NewGuid(),FullName="Old unknown",NormalizedFullName="old unknown",Phone="01012345678",Area="منطقة مجهولة"};db.Add(old);await db.SaveChangesAsync();
        (await client.PutAsJsonAsync($"/api/StudentCommands/{old.Id}",new {id=old.Id,notes="Other field updated"})).EnsureSuccessStatusCode();db.ChangeTracker.Clear();Assert.Equal("منطقة مجهولة",(await db.Students.SingleAsync(s=>s.Id==old.Id)).Area);
        // An explicitly submitted unchanged legacy area is normalized too; omission preserves PATCH semantics.
        (await client.PutAsJsonAsync($"/api/StudentCommands/{old.Id}",new {id=old.Id,area="منطقة مجهولة"})).EnsureSuccessStatusCode();db.ChangeTracker.Clear();Assert.Equal("اخرى",(await db.Students.SingleAsync(s=>s.Id==old.Id)).Area);
    }
}
