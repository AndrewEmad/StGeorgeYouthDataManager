using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Students.Service.Implementations.Commands;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class BulkAreaTests(SqlFixture fixture)
{
    private static Student Student(string name,Guid? owner=null) => new() {Id=Guid.NewGuid(),FullName=name,NormalizedFullName=name,Phone="01012345678",SecondaryPhone="+447700900123",Address="Preserve address",AcademicYear="سنة سابعة",Area="شبرا البلد",Notes="Preserve notes",ServantId=owner,BirthMonth=3};
    private static SelectedStudentVersion Selected(Student student) => new(student.Id,Convert.ToBase64String(student.Version));
    private async Task<HttpClient> Client(IsolatedApiFactory factory,ApplicationUser actor)
    {
        var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=actor.UserName,password="password123"});login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        client.DefaultRequestHeaders.Add("X-Audit-Reason","Area group change");return client;
    }
    [Theory]
    [InlineData("Admin")]
    [InlineData("Manager")]
    [InlineData("Priest")]
    public async Task Privileged_area_batch_preserves_other_fields_reports_missing_archived_and_stale_rows_and_replays_safely(string role)
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,role);var db=p.GetRequiredService<AppDbContext>();
        var first=Student("Year first",actor.Id);var stale=Student("Year stale",actor.Id);var archived=Student("Year archived");archived.IsDeleted=true;
        db.AddRange(first,stale,archived);await db.SaveChangesAsync();var selectedFirst=Selected(first);var selectedStale=Selected(stale);
        stale.Area="العبور";stale.Notes="Concurrent notes";await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=await Client(factory,actor);
        var missing=Guid.NewGuid();var request=new BulkAreaRequest([selectedFirst,selectedStale,Selected(archived),new(missing,selectedFirst.Version)],"  المساكن  ");
        var response=await client.PostAsJsonAsync("/api/StudentCommands/bulk-area?serviceYear=2024",request);response.EnsureSuccessStatusCode();
        var result=(await response.Content.ReadFromJsonAsync<BulkAreaResult>())!;Assert.Equal(4,result.Outcomes.Count);
        Assert.True(result.Outcomes[0].Success);Assert.True(result.Outcomes[0].Changed);Assert.Equal("Conflict",result.Outcomes[1].Code);Assert.Equal("العبور",result.Outcomes[1].Area);
        Assert.Equal("NotFound",result.Outcomes[2].Code);Assert.Equal("NotFound",result.Outcomes[3].Code);
        db.ChangeTracker.Clear();var saved=await db.Students.SingleAsync(s=>s.Id==first.Id);Assert.Equal("الوحدة",saved.Area);
        Assert.Equal("سنة سابعة",saved.AcademicYear);Assert.Equal("Preserve address",saved.Address);Assert.Equal(first.Phone,saved.Phone);Assert.Equal(first.SecondaryPhone,saved.SecondaryPhone);Assert.Equal("Preserve notes",saved.Notes);Assert.Equal(actor.Id,saved.ServantId);Assert.Equal(3,saved.BirthMonth);
        var edit=await db.StudentEditLogs.SingleAsync(e=>e.StudentId==first.Id);Assert.Equal(actor.Id,edit.UpdatedByUserId);Assert.Equal(actor.FullName,edit.UpdatedByUserName);Assert.Contains("شبرا البلد",edit.Details);Assert.Contains("الوحدة",edit.Details);
        var audit=await db.Set<AuditEntry>().SingleAsync(a=>a.EntityType==nameof(Student) && a.EntityId==first.Id.ToString() && a.Action=="Modified");
        Assert.Equal(actor.Id,audit.ActorId);Assert.Equal("Area group change",audit.Reason);Assert.False(string.IsNullOrWhiteSpace(audit.CorrelationId));
        using(var before=JsonDocument.Parse(audit.BeforeJson))Assert.Equal("شبرا البلد",before.RootElement.GetProperty("Area").GetString());
        using(var after=JsonDocument.Parse(audit.AfterJson)){Assert.Equal("الوحدة",after.RootElement.GetProperty("Area").GetString());Assert.False(after.RootElement.TryGetProperty("Phone",out _));}
        var retry=await client.PostAsJsonAsync("/api/StudentCommands/bulk-area",new BulkAreaRequest([selectedFirst],"المساكن"));retry.EnsureSuccessStatusCode();
        Assert.False((await retry.Content.ReadFromJsonAsync<BulkAreaResult>())!.Outcomes.Single().Changed);
        Assert.Equal(1,await db.StudentEditLogs.CountAsync(e=>e.StudentId==first.Id));
        var reviewed=await client.PostAsJsonAsync("/api/StudentCommands/bulk-area",new BulkAreaRequest([new(stale.Id,result.Outcomes[1].Version)],"المساكن"));reviewed.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear();Assert.Equal("Concurrent notes",(await db.Students.SingleAsync(s=>s.Id==stale.Id)).Notes);
    }
    [Theory]
    [InlineData("Servant")]
    [InlineData("Secretary")]
    public async Task Assigned_users_cannot_access_privileged_bulk_area_edit(string role)
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,role);var db=p.GetRequiredService<AppDbContext>();var student=Student("Restricted",actor.Id);db.Add(student);await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=await Client(factory,actor);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/StudentCommands/bulk-area",new BulkAreaRequest([Selected(student)],"المساكن"))).StatusCode);
        db.ChangeTracker.Clear();Assert.Equal("شبرا البلد",(await db.Students.SingleAsync(s=>s.Id==student.Id)).Area);
    }
    [Fact]
    public async Task Unlisted_bulk_area_is_saved_as_other_and_blank_targets_remain_invalid()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var actor=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();var student=Student("Fallback area");db.Add(student);await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=await Client(factory,actor);
        var response=await client.PostAsJsonAsync("/api/StudentCommands/bulk-area",new BulkAreaRequest([Selected(student)],"منطقة غير مدرجة"));response.EnsureSuccessStatusCode();
        Assert.Equal("اخرى",(await response.Content.ReadFromJsonAsync<BulkAreaResult>())!.Outcomes.Single().Area);db.ChangeTracker.Clear();var saved=await db.Students.SingleAsync(s=>s.Id==student.Id);Assert.Equal("اخرى",saved.Area);Assert.Equal("سنة سابعة",saved.AcademicYear);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/StudentCommands/bulk-area",new BulkAreaRequest([Selected(saved)]," "))).StatusCode);
    }
    [Fact]
    public async Task Validation_and_transaction_rollback_prevent_partial_persistence_or_audit()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var db=p.GetRequiredService<AppDbContext>();var caller=p.GetRequiredService<TestCaller>();var store=p.GetRequiredService<IApplicationStore>();var actor=await fixture.User(p,"Manager");caller.UserId=actor.Id;
        var student=Student("Rollback");student.Phone="legacy invalid phone";db.Add(student);await db.SaveChangesAsync();var selected=Selected(student);var commands=new StudentCommandsService(store,caller);
        foreach(var invalid in new[]{new BulkAreaRequest([selected]," "),new([selected],new string('x',101)),new([selected,selected],"الوحدة"),new([],"الوحدة"),new(null,"الوحدة"),new([new(Guid.Empty,selected.Version)],"الوحدة"),new([new(student.Id,"bad")],"الوحدة"),new(Enumerable.Repeat(selected,5001).ToList(),"الوحدة")})
            Assert.Equal(ServiceResultStatus.Failure,(await commands.UpdateAreas(invalid)).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>store.Transaction<int>(async()=>{var result=await commands.UpdateAreas(new([selected],"المساكن"));Assert.Equal(ServiceResultStatus.Success,result.Status);throw new InvalidOperationException("Simulated enclosing operation failure");}));
        db.ChangeTracker.Clear();Assert.Equal("شبرا البلد",(await db.Students.SingleAsync(s=>s.Id==student.Id)).Area);Assert.False(await db.StudentEditLogs.AnyAsync(e=>e.StudentId==student.Id));Assert.False(await db.Set<AuditEntry>().AnyAsync(a=>a.EntityId==student.Id.ToString() && a.Action=="Modified"));
        // A focused year change must not revalidate/overwrite unrelated legacy fields.
        Assert.Equal(ServiceResultStatus.Success,(await commands.UpdateAreas(new([selected],"اخرى"))).Status);
        Assert.Equal("legacy invalid phone",(await db.Students.SingleAsync(s=>s.Id==student.Id)).Phone);
    }
}
