using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class ApprovalRetryTests(SqlFixture fixture)
{
    private static async Task<HttpClient> Login(IsolatedApiFactory factory,ApplicationUser user)
    {
        var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var result=await client.PostAsJsonAsync("/api/Auth/login",new {username=user.UserName,password="password123"});result.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await result.Content.ReadFromJsonAsync<LoginResponse>())!.Token);return client;
    }
    [Fact]
    public async Task Addition_retries_share_request_and_approval_retries_share_created_student_with_full_history()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var requester=await fixture.User(p);var admin=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();await using var factory=new IsolatedApiFactory(fixture.Connection);using var user=await Login(factory,requester);using var processor=await Login(factory,admin);
        var name="Addition_"+Guid.NewGuid();var payload=new {fullName=name,phone="01012345678",secondaryPhone="+447700900123",address="Full address",confessionFather="Father",birthDate="2000-02-29",birthMonth=6,notes="Notes",gender=1};
        var first=await user.PostAsJsonAsync("/api/StudentAdditionRequests",payload);first.EnsureSuccessStatusCode();using var firstBody=JsonDocument.Parse(await first.Content.ReadAsStringAsync());var id=firstBody.RootElement.GetProperty("id").GetGuid();var again=await user.PostAsJsonAsync("/api/StudentAdditionRequests",payload);again.EnsureSuccessStatusCode();Assert.Contains(id.ToString(),await again.Content.ReadAsStringAsync());
        var request=await db.StudentAdditionRequests.AsNoTracking().SingleAsync(r=>r.Id==id);processor.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",Convert.ToBase64String(request.Version));
        Assert.Equal(HttpStatusCode.OK,(await processor.PostAsJsonAsync($"/api/StudentAdditionRequests/{id}/approve",new{})).StatusCode);Assert.Equal(HttpStatusCode.OK,(await processor.PostAsJsonAsync($"/api/StudentAdditionRequests/{id}/approve",new{})).StatusCode);
        var approved=await db.StudentAdditionRequests.AsNoTracking().SingleAsync(r=>r.Id==id);Assert.NotNull(approved.CreatedStudentId);Assert.Equal(1,await db.Students.CountAsync(s=>s.FullName==name));var student=await db.Students.SingleAsync(s=>s.Id==approved.CreatedStudentId);Assert.Equal(2,student.BirthMonth);Assert.Equal(payload.secondaryPhone,student.SecondaryPhone);
        var history=await user.GetAsync($"/api/approvals/addition?requestId={id}");history.EnsureSuccessStatusCode();var json=await history.Content.ReadAsStringAsync();Assert.Contains("Full address",json);Assert.Contains("Father",json);Assert.Contains(approved.CreatedStudentId.ToString()!,json);
    }
    [Fact]
    public async Task Stale_assignment_approval_preserves_current_owner_and_requester_can_withdraw_with_audit()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var requester=await fixture.User(p);var owner=await fixture.User(p);var admin=await fixture.User(p,"Manager");var db=p.GetRequiredService<AppDbContext>();var student=new Student {Id=Guid.NewGuid(),FullName="Stale",NormalizedFullName="stale",Phone="01012345678"};db.Add(student);await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var user=await Login(factory,requester);using var processor=await Login(factory,admin);var response=await user.PostAsJsonAsync("/api/StudentAssignmentRequests",new {studentId=student.Id});response.EnsureSuccessStatusCode();using var body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());var id=body.RootElement.GetProperty("id").GetGuid();
        student.ServantId=owner.Id;await db.SaveChangesAsync();Assert.Equal(HttpStatusCode.Conflict,(await processor.PostAsJsonAsync($"/api/StudentAssignmentRequests/{id}/approve",new{})).StatusCode);
        db.ChangeTracker.Clear();Assert.Equal(owner.Id,(await db.Students.SingleAsync(s=>s.Id==student.Id)).ServantId);
        var request=await db.StudentAssignmentRequests.AsNoTracking().SingleAsync(r=>r.Id==id);user.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",Convert.ToBase64String(request.Version));user.DefaultRequestHeaders.Add("X-Audit-Reason","Withdraw stale request");var withdrawal=await user.PostAsJsonAsync($"/api/approvals/assignment/{id}/withdraw",new {reason="Withdraw stale request"});Assert.Equal(HttpStatusCode.OK,withdrawal.StatusCode);
        Assert.Equal(AssignmentRequestStatus.Cancelled,(await db.StudentAssignmentRequests.AsNoTracking().SingleAsync(r=>r.Id==id)).Status);Assert.True(await db.Set<AuditEntry>().AnyAsync(a=>a.EntityId==id.ToString() && a.ActorId==requester.Id));
    }
}
