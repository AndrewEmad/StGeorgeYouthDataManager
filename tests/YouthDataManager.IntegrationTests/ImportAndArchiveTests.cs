using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Implementations.Commands;
namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class ImportAndArchiveTests(SqlFixture fixture)
{
    private async Task<HttpClient> Client(IsolatedApiFactory factory,ApplicationUser user)
    {
        var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var response=await client.PostAsJsonAsync("/api/Auth/login",new {username=user.UserName,password="password123"});response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization=new("Bearer",(await response.Content.ReadFromJsonAsync<LoginResponse>())!.Token);return client;
    }
    [Fact]
    public async Task Invalid_import_row_does_not_poison_valid_row_and_lost_response_retries_return_same_created_ID()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var user=await fixture.User(scope.ServiceProvider,"Manager");var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=await Client(factory,user);var op=Guid.NewGuid();var name="Import_"+Guid.NewGuid();
        var payload=new {operationId=op,rows=new[]{new {row=2,student=new {fullName="",phone="01012345678",gender=0},duplicateReviewed=true},new {row=7,student=new {fullName=name,phone="01012345678",gender=0},duplicateReviewed=true}}};
        var first=await client.PostAsJsonAsync("/api/StudentCommands/bulk-import-v2",payload);Assert.Equal(HttpStatusCode.OK,first.StatusCode);using var body=JsonDocument.Parse(await first.Content.ReadAsStringAsync());var id=body.RootElement.GetProperty("outcomes")[1].GetProperty("studentId").GetGuid();
        var retry=await client.PostAsJsonAsync("/api/StudentCommands/bulk-import-v2",payload);Assert.Equal(HttpStatusCode.OK,retry.StatusCode);using var again=JsonDocument.Parse(await retry.Content.ReadAsStringAsync());Assert.Equal(id,again.RootElement.GetProperty("outcomes")[1].GetProperty("studentId").GetGuid());Assert.True(again.RootElement.GetProperty("outcomes")[1].GetProperty("replayed").GetBoolean());
        Assert.Equal(1,await db.Students.CountAsync(s=>s.FullName==name));Assert.Equal(2,await db.Set<ImportRowOutcome>().CountAsync(r=>r.OperationId==op));
    }
    [Fact]
    public async Task Phone_variants_are_duplicate_candidates_and_archival_restore_preserves_history_without_reopening_work()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var manager=await fixture.User(p,"Manager");var owner=await fixture.User(p);var db=p.GetRequiredService<AppDbContext>();
        var s=new Student {Id=Guid.NewGuid(),FullName="Archive_"+Guid.NewGuid(),NormalizedFullName="Archive",Phone="+44 7700 900123",ServantId=owner.Id};db.Add(s);db.Add(new FollowUpTask {StudentId=s.Id,AssigneeId=owner.Id});db.Add(new CallLog {Id=Guid.NewGuid(),StudentId=s.Id,ServantId=owner.Id,CallDate=DateTime.UtcNow});await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=await Client(factory,manager);
        var preview=await client.PostAsJsonAsync("/api/StudentCommands/import-preview",new {operationId=Guid.NewGuid(),rows=new[]{new {row=3,student=new {fullName="Different name",phone="00447700900123",gender=0}}}});Assert.Equal(HttpStatusCode.OK,preview.StatusCode);Assert.Contains(s.Id.ToString(),await preview.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add("X-Audit-Reason","Archive with evidence");client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",Convert.ToBase64String(s.Version));Assert.Equal(HttpStatusCode.NoContent,(await client.DeleteAsync($"/api/StudentCommands/{s.Id}")).StatusCode);
        db.ChangeTracker.Clear();var archived=await db.Students.IgnoreQueryFilters().SingleAsync(x=>x.Id==s.Id);Assert.True(archived.IsDeleted);Assert.All(await db.Set<FollowUpTask>().Where(t=>t.StudentId==s.Id).ToListAsync(),t=>Assert.Equal("Cancelled",t.Status));Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/api/activity-history/calls?studentId={s.Id}")).StatusCode);
        client.DefaultRequestHeaders.Remove("If-Match");client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",Convert.ToBase64String(archived.Version));Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync($"/api/StudentCommands/{s.Id}/restore",new{})).StatusCode);
        db.ChangeTracker.Clear();var restored=await db.Students.SingleAsync(x=>x.Id==s.Id);Assert.Null(restored.ServantId);Assert.All(await db.Set<FollowUpTask>().Where(t=>t.StudentId==s.Id).ToListAsync(),t=>Assert.Equal("Cancelled",t.Status));Assert.True(await db.CallLogs.IgnoreQueryFilters().AnyAsync(c=>c.StudentId==s.Id));
    }
    [Fact]
    public async Task Historical_eligibility_review_requires_preserved_positive_attendance_and_never_infers_old_ownership()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var store=scope.ServiceProvider.GetRequiredService<IApplicationStore>();var owner=await fixture.User(scope.ServiceProvider);var s=new Student {Id=Guid.NewGuid(),FullName="Historical",NormalizedFullName="historical",Phone="01012345678",ServantId=owner.Id};store.Add(s);var m=new MeetingSession {Date=new(2024,4,5),Status="Finalized",RosterKnown=false};store.Add(m);store.Add(new MeetingRosterEntry {SessionId=m.Id,PersonId=s.Id,Name=s.FullName,Present=true,AssignedServantId=owner.Id});await store.Save();var service=new MeetingService(store);
        Assert.Equal(ServiceResultStatus.Failure,(await service.ReviewEligibility(m.Id,[],[],"Original paper roster")).Status);
        Assert.Equal(ServiceResultStatus.Success,(await service.ReviewEligibility(m.Id,[s.Id],[],"Original paper roster")).Status);
        var row=await store.Query<MeetingRosterEntry>(false).SingleAsync(r=>r.SessionId==m.Id);Assert.True(row.Present);Assert.Null(row.AssignedServantId);Assert.True((await store.Query<MeetingSession>(false).SingleAsync(x=>x.Id==m.Id)).RosterKnown);
    }
}
