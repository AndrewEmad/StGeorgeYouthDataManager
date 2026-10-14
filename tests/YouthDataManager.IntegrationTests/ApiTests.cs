using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.WebApi.BackgroundWorkers;

namespace YouthDataManager.IntegrationTests;

public class IsolatedApiFactory(string connection, string environment="Testing", bool? initializeOnStartup=false) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        if (initializeOnStartup.HasValue) builder.UseSetting("Database:InitializeOnStartup",initializeOnStartup.Value.ToString());
        builder.UseSetting("NotificationWorker:Enabled","false");
        builder.ConfigureServices(services=>{
            services.RemoveAll<DbContextOptions<AppDbContext>>();services.AddDbContext<AppDbContext>(o=>o.UseSqlServer(connection));
            foreach(var worker in services.Where(s=>s.ServiceType==typeof(IHostedService) && s.ImplementationType==typeof(NotificationReminderWorker)).ToArray())services.Remove(worker);
        });
    }
}
[Collection("SQL")]
public sealed class ApiTests(SqlFixture fixture)
{
    [Fact]
    public async Task Servant_workspace_scopes_manager_reports_tasks_requests_and_photos_without_changing_admin_authority()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;
        var manager=await fixture.User(p,"Manager");var other=await fixture.User(p);
        var db=p.GetRequiredService<AppDbContext>();var area="personal_"+Guid.NewGuid().ToString("N");
        var own=new Student{Id=Guid.NewGuid(),FullName="My group",Phone="01012345678",Area=area,ServantId=manager.Id};
        var unrelated=new Student{Id=Guid.NewGuid(),FullName="Another group private",Phone="01012345678",Area=area,ServantId=other.Id};
        var unassigned=new Student{Id=Guid.NewGuid(),FullName="Discovery only",Phone="private",Address="private",Area=area};
        var untouched=new Student{Id=Guid.NewGuid(),FullName="Only another servant called",Phone="01012345678",Area=area+"_other",ServantId=manager.Id};
        db.AddRange(own,unrelated,unassigned,untouched);await db.SaveChangesAsync();var at=DateTime.UtcNow;
        db.AddRange(new CallLog{Id=Guid.NewGuid(),StudentId=own.Id,ServantId=manager.Id,CallDate=at,CallStatus=CallStatus.NotAnswered},
            new CallLog{Id=Guid.NewGuid(),StudentId=untouched.Id,ServantId=other.Id,CallDate=at,CallStatus=CallStatus.Answered},
            new CallLog{Id=Guid.NewGuid(),StudentId=own.Id,ServantId=other.Id,CallDate=at,CallStatus=CallStatus.Answered});
        var shared=new HomeVisit{Id=Guid.NewGuid(),StudentId=own.Id,ServantId=other.Id,VisitDate=at,VisitOutcome=VisitOutcome.Successful};
        shared.Participants.Add(new HomeVisitParticipant{HomeVisitId=shared.Id,ServantId=manager.Id});
        db.AddRange(shared,new HomeVisit{Id=Guid.NewGuid(),StudentId=own.Id,ServantId=other.Id,VisitDate=at,VisitOutcome=VisitOutcome.Successful},
            new FollowUpTask{StudentId=own.Id,AssigneeId=manager.Id,Status="Open",DueAt=at},
            new FollowUpTask{StudentId=own.Id,AssigneeId=other.Id,Status="Open",DueAt=at},
            new StudentAdditionRequest{Id=Guid.NewGuid(),FullName="My request",Phone="01012345678",RequestedByUserId=manager.Id,RequestedAt=at},
            new StudentAdditionRequest{Id=Guid.NewGuid(),FullName="Other private request",Phone="01012345678",RequestedByUserId=other.Id,RequestedAt=at});
        await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        var login=await Login(client,manager);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        client.DefaultRequestHeaders.Add("X-YDM-Workspace","personal");
        var report=(await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/reports/suite/contact?area={area}&serviceYear=all"));
        Assert.Equal("افتقادي لمجموعتي",report.GetProperty("title").GetString());
        Assert.Equal(1,report.GetProperty("totalCount").GetInt32());
        var row=report.GetProperty("rows")[0];Assert.Equal(own.Id.ToString(),row.GetProperty("id").GetString());Assert.Equal(2,row.GetProperty("attempts").GetInt32());
        var totals=report.GetProperty("totals");Assert.Equal(0,totals.GetProperty("callAnswered").GetInt32());Assert.Equal(1,totals.GetProperty("callNotAnswered").GetInt32());Assert.Equal(1,totals.GetProperty("visitSuccessful").GetInt32());
        var printed=await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/reports/suite/contact?area={area}&full=true&serviceYear=all");Assert.Equal(report.GetProperty("rows").ToString(),printed.GetProperty("rows").ToString());
        foreach(var format in new[]{"csv","xlsx"})
        {
            var export=await client.GetAsync($"/api/reports/suite/contact/export?area={area}&format={format}&serviceYear=all");Assert.Equal(HttpStatusCode.OK,export.StatusCode);
            if(format=="csv"){var content=await export.Content.ReadAsStringAsync();Assert.Contains("My group",content);Assert.DoesNotContain("Another group",content);Assert.DoesNotContain("Discovery only",content);}
            else {using var book=new ClosedXML.Excel.XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));Assert.Equal(2,book.Worksheet(1).LastRowUsed()!.RowNumber());}
        }
        var dashboard=await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/Reports/dashboard/servant?serviceYear=all");Assert.Equal(1,dashboard.GetProperty("studentsNotContacted").GetInt32());
        var missing=await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/reports/suite/contact?area={area}_other&issue=never-attempted&serviceYear=all");Assert.Equal(1,missing.GetProperty("totalCount").GetInt32());Assert.Equal(0,missing.GetProperty("rows")[0].GetProperty("attempts").GetInt32());
        foreach(var key in new[]{"workload","workload-owners","approvals","meeting-trends","servant-service-summary","group-attendance","pastoral-follow-up"})
        {
            Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/reports/suite/{key}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/reports/suite/{key}/export?format=csv")).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/reports/suite/contact?servantId={other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/Reports/dashboard/manager")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/StudentAdditionRequests/pending")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/StudentQueries/{unrelated.Id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/StudentQueries/{unrelated.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/follow-up-tasks?unassigned=true")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/approvals/all?mine=false")).StatusCode);
        var tasks=await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/follow-up-tasks?serviceYear=all");Assert.Equal(1,tasks.GetProperty("totalCount").GetInt32());
        var requests=await client.GetStringAsync("/api/approvals/all?serviceYear=all");Assert.Contains("My request",requests);Assert.DoesNotContain("Other private request",requests);
        var group=await client.GetStringAsync($"/api/StudentQueries/students-for-servant?area={area}&segment=Assigned");Assert.Contains("My group",group);Assert.DoesNotContain("Discovery only",group);Assert.DoesNotContain("Another group",group);
        var discovery=await client.GetStringAsync($"/api/StudentQueries/students-for-servant?area={area}&segment=Unassigned");Assert.Contains("Discovery only",discovery);Assert.DoesNotContain("private",discovery);Assert.DoesNotContain("My group",discovery);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/StudentQueries/students-for-servant?segment=invalid")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-YDM-Workspace");
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/api/StudentQueries/{unrelated.Id}")).StatusCode);
        var admin=await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/reports/suite/contact?area={area}&serviceYear=all");Assert.Equal(3,admin.GetProperty("totalCount").GetInt32());Assert.Equal(1,admin.GetProperty("totals").GetProperty("callAnswered").GetInt32());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/Reports/dashboard/manager")).StatusCode);
    }

    private async Task<LoginResponse> Login(HttpClient client,ApplicationUser user)
    {
        var response=await client.PostAsJsonAsync("/api/Auth/login",new {username=user.UserName,password="password123"});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }
    [Fact]
    public async Task Api_enforces_resource_scope_privileged_commands_minimal_discovery_and_student_attendance_rosters()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var owner=await fixture.User(p);var stranger=await fixture.User(p);var secretary=await fixture.User(p,"Secretary");
        var db=p.GetRequiredService<AppDbContext>();var assigned=new Student {Id=Guid.NewGuid(),FullName="API private",NormalizedFullName="api private",Phone="01012345678",ServantId=owner.Id};var discovery=new Student {Id=Guid.NewGuid(),FullName="API discovery",NormalizedFullName="api discovery",Phone="private",Address="private",Notes="private"};db.AddRange(assigned,discovery);await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await Login(client,stranger);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/StudentQueries/{assigned.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/StudentQueries/{assigned.Id}/photo")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/StudentCommands/{assigned.Id}",new {id=assigned.Id,fullName="Changed",phone="01012345678",gender=0})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/StudentCommands",new {fullName="Forbidden",phone="01012345678",gender=0})).StatusCode);
        var minimal=await client.GetStringAsync("/api/StudentQueries/unassigned-for-servant?pageSize=100");Assert.DoesNotContain("private",minimal);Assert.DoesNotContain("secondaryPhone",minimal);Assert.DoesNotContain("notes",minimal);
        var sec=await Login(client,secretary);client.DefaultRequestHeaders.Authorization=new("Bearer",sec.Token);
        client.DefaultRequestHeaders.Add("X-YDM-Workspace","personal");
        var started=await client.PostAsJsonAsync("/api/meetings",new {date="2026-10-09"});Assert.Equal(HttpStatusCode.OK,started.StatusCode);var meeting=(await started.Content.ReadFromJsonAsync<MeetingSession>())!;
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync($"/api/meetings/{meeting.Id}/roster?kind=Student")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/meetings/{meeting.Id}/roster?kind=Servant")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PutAsJsonAsync($"/api/meetings/{meeting.Id}/marks",new {kind="Servant",ids=new[]{owner.Id},present=true})).StatusCode);
    }
    [Fact]
    public async Task Onboarding_replacement_revocation_and_anonymous_logout_are_server_confirmed()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var user=await fixture.User(p);var db=p.GetRequiredService<AppDbContext>();user.MustChangePassword=true;user.Address="";await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await Login(client,user);Assert.True(login.RequiresPasswordChange);Assert.True(login.RequiresProfileCompletion);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/reports/dashboard/servant")).StatusCode);
        var changed=await client.PostAsJsonAsync("/api/Auth/change-password",new {currentPassword="password123",newPassword="New_password123!"});Assert.Equal(HttpStatusCode.OK,changed.StatusCode);var password=(await changed.Content.ReadFromJsonAsync<LoginResponse>())!;Assert.False(password.RequiresPasswordChange);Assert.True(password.RequiresProfileCompletion);
        client.DefaultRequestHeaders.Authorization=new("Bearer",password.Token);var profile=await client.PutAsJsonAsync("/api/Profile",new {fullName="Validated profile",phone="01012345678",address="Cairo"});Assert.Equal(HttpStatusCode.OK,profile.StatusCode);var completed=(await profile.Content.ReadFromJsonAsync<LoginResponse>())!;Assert.False(completed.RequiresProfileCompletion);Assert.Equal("Validated profile",completed.FullName);
        client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/Profile")).StatusCode);
        client.DefaultRequestHeaders.Authorization=null;Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/Auth/logout",new {refreshToken=completed.RefreshToken})).StatusCode);
        client.DefaultRequestHeaders.Authorization=new("Bearer",completed.Token);Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/Profile")).StatusCode);
    }
    [Fact]
    public async Task Approval_history_queries_process_nonempty_enum_statuses_and_archived_targets_in_SQL()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var user=await fixture.User(p);var db=p.GetRequiredService<AppDbContext>();var student=new Student {Id=Guid.NewGuid(),FullName="Archived history",NormalizedFullName="archived history",Phone="01012345678",IsDeleted=true};db.Add(student);db.Add(new StudentAssignmentRequest {Id=Guid.NewGuid(),StudentId=student.Id,RequestedByUserId=user.Id,RequestedAt=DateTime.UtcNow,Status=AssignmentRequestStatus.Approved});await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await Login(client,user);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        var response=await client.GetAsync("/api/approvals/assignment?status=Approved");Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("Archived history",await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/approvals/addition?mine=false")).StatusCode);
    }

    [Fact]
    public async Task Profile_validation_preserves_failed_input_and_optional_address_can_be_cleared()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var user=await fixture.User(scope.ServiceProvider,"Admin");
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        var login=await Login(client,user);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PutAsJsonAsync("/api/Profile",new {fullName="Valid",phone="12abc34567",email="invalid"})).StatusCode);
        var saved=await client.GetStringAsync("/api/Profile");Assert.Contains("01012345678",saved);
        var cleared=await client.PutAsJsonAsync("/api/Profile",new {fullName="Confirmed admin",phone="+44 (7700).900123",address="",email=""});Assert.Equal(HttpStatusCode.OK,cleared.StatusCode);
        var replacement=(await cleared.Content.ReadFromJsonAsync<LoginResponse>())!;Assert.False(replacement.RequiresProfileCompletion);Assert.Equal("Confirmed admin",replacement.FullName);
        client.DefaultRequestHeaders.Authorization=new("Bearer",replacement.Token);using var profile=System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/api/Profile"));Assert.True(string.IsNullOrEmpty(profile.RootElement.GetProperty("address").GetString()));
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Entry(user).ReloadAsync();
        Assert.True((await scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>().RemoveFromRoleAsync(user,"Admin")).Succeeded);
    }

    [Fact]
    public async Task Bulk_assignment_commits_valid_rows_and_reports_failed_rows_without_poisoning_the_retry()
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var manager=await fixture.User(p,"Manager");var owner=await fixture.User(p);var db=p.GetRequiredService<AppDbContext>();
        var student=new Student {Id=Guid.NewGuid(),FullName="Partial batch",NormalizedFullName="partial batch",Phone="01012345678"};db.Add(student);await db.SaveChangesAsync();var missing=Guid.NewGuid();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});var login=await Login(client,manager);client.DefaultRequestHeaders.Authorization=new("Bearer",login.Token);
        var response=await client.PostAsJsonAsync("/api/StudentCommands/bulk-assignment",new {ids=new[]{missing,student.Id},servantId=owner.Id});Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var json=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());var outcomes=json.RootElement.GetProperty("outcomes");Assert.False(outcomes[0].GetProperty("success").GetBoolean());Assert.True(outcomes[1].GetProperty("success").GetBoolean());
        await db.Entry(student).ReloadAsync();Assert.Equal(owner.Id,student.ServantId);
        var retry=await client.PostAsJsonAsync("/api/StudentCommands/bulk-assignment",new {ids=new[]{student.Id},servantId=owner.Id});Assert.Equal(HttpStatusCode.OK,retry.StatusCode);
        var task=new FollowUpTask {StudentId=student.Id,AssigneeId=owner.Id,Status="Open"};var history=new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=owner.Id,CallDate=DateTime.UtcNow};db.AddRange(task,history);await db.SaveChangesAsync();
        var unassign=await client.PostAsJsonAsync("/api/StudentCommands/bulk-assignment",new {ids=new[]{missing,student.Id},servantId=(Guid?)null});unassign.EnsureSuccessStatusCode();
        using var unassignJson=System.Text.Json.JsonDocument.Parse(await unassign.Content.ReadAsStringAsync());var unassignOutcomes=unassignJson.RootElement.GetProperty("outcomes");Assert.False(unassignOutcomes[0].GetProperty("success").GetBoolean());Assert.True(unassignOutcomes[1].GetProperty("success").GetBoolean());
        await db.Entry(student).ReloadAsync();await db.Entry(task).ReloadAsync();await db.Entry(history).ReloadAsync();Assert.Null(student.ServantId);Assert.False(student.IsDeleted);Assert.Null(task.AssigneeId);Assert.Equal("Open",task.Status);Assert.Equal(owner.Id,history.ServantId);

    }
}
