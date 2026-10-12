using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class UnifiedApprovalTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("Manager")][InlineData("Admin")][InlineData("Priest")][InlineData("Secretary")][InlineData("Servant")]
    public async Task Mixed_feed_scopes_all_kinds_before_SQL_paging_and_retains_detail_and_action_contracts(string role)
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var p=scope.ServiceProvider;var user=await fixture.User(p,role);var other=await fixture.User(p);var db=p.GetRequiredService<AppDbContext>();
        var target=new Student {Id=Guid.NewGuid(),FullName="Archived target",NormalizedFullName="archived target",Phone="01012345678",IsDeleted=true};
        var now=DateTime.UtcNow;
        var addition=new StudentAdditionRequest {Id=Guid.NewGuid(),RequestedByUserId=user.Id,RequestedAt=now,FullName="Complete addition",Phone="01012345678",Address="Full address",SecondaryPhone="+447700900123",ConfessionFather="Father",BirthDate=new(2001,5,7),BirthMonth=5,Notes="Original notes"};
        var assignment=new StudentAssignmentRequest {Id=Guid.NewGuid(),StudentId=target.Id,RequestedByUserId=user.Id,RequestedAt=now,Status=AssignmentRequestStatus.Approved,ProcessedAt=now,ProcessedByUserId=other.Id,ProcessingReason="Accepted"};
        var removal=new StudentRemovalRequest {Id=Guid.NewGuid(),StudentId=target.Id,RequestedByUserId=user.Id,RequestedAt=now.AddMinutes(-1)};
        var rejected=new StudentAdditionRequest {Id=Guid.NewGuid(),RequestedByUserId=user.Id,RequestedAt=now.AddMinutes(-2),FullName="Rejected addition",Phone="01012345678",Status=AssignmentRequestStatus.Rejected};
        var foreign=new StudentAdditionRequest {Id=Guid.NewGuid(),RequestedByUserId=other.Id,RequestedAt=now.AddDays(1),FullName="Other request",Phone="01012345678"};
        db.AddRange(target,addition,assignment,removal,rejected,foreign);await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=user.UserName,password="password123"});login.EnsureSuccessStatusCode();client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        var pages=new List<JsonElement>();
        for(var page=1;page<=2;page++){var response=await client.GetAsync($"/api/approvals/all?page={page}&pageSize=2");response.EnsureSuccessStatusCode();using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());Assert.Equal(4,json.RootElement.GetProperty("totalCount").GetInt32());pages.AddRange(json.RootElement.GetProperty("items").EnumerateArray().Select(x=>x.Clone()));}
        Assert.Equal(4,pages.Select(x=>x.GetProperty("id").GetGuid()).Distinct().Count());Assert.DoesNotContain(pages,x=>x.GetProperty("id").GetGuid()==foreign.Id);
        Assert.All(pages,x=>Assert.Equal(user.Id,x.GetProperty("requestedByUserId").GetGuid()));Assert.Equal(new[]{"addition","assignment","removal"},pages.Select(x=>x.GetProperty("kind").GetString()).Distinct().OrderBy(x=>x));
        Assert.Equal("Full address",pages.Single(x=>x.GetProperty("id").GetGuid()==addition.Id).GetProperty("address").GetString());Assert.Equal("Archived target",pages.Single(x=>x.GetProperty("id").GetGuid()==assignment.Id).GetProperty("fullName").GetString());
        var repeated=await client.GetStringAsync("/api/approvals/all?page=1&pageSize=2");using(var json=JsonDocument.Parse(repeated))Assert.Equal(pages.Take(2).Select(x=>x.GetProperty("id").GetGuid()),json.RootElement.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetGuid()));
        using(var pending=JsonDocument.Parse(await client.GetStringAsync("/api/approvals/all?status=Pending"))){Assert.Equal(2,pending.RootElement.GetProperty("totalCount").GetInt32());Assert.All(pending.RootElement.GetProperty("items").EnumerateArray(),x=>Assert.Equal("Pending",x.GetProperty("status").GetString()));}
        foreach(var kind in new[]{"addition","assignment","removal"}){using var json=JsonDocument.Parse(await client.GetStringAsync($"/api/approvals/{kind}"));Assert.All(json.RootElement.GetProperty("items").EnumerateArray(),x=>Assert.Equal(kind,x.GetProperty("kind").GetString()));}
        var global=await client.GetAsync($"/api/approvals/all?mine=false&requestId={foreign.Id}");
        if(role is "Manager" or "Admin" or "Priest"){global.EnsureSuccessStatusCode();Assert.Contains(foreign.Id.ToString(),await global.Content.ReadAsStringAsync());}else Assert.Equal(HttpStatusCode.Forbidden,global.StatusCode);
        using(var restricted=JsonDocument.Parse(await client.GetStringAsync($"/api/approvals/all?requestId={foreign.Id}")))Assert.Equal(0,restricted.RootElement.GetProperty("totalCount").GetInt32());
        client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match",Convert.ToBase64String(removal.Version));
        const string reason="تم حل الطلب ومراجعة البيانات";client.DefaultRequestHeaders.Add("X-Audit-Reason",Uri.EscapeDataString(reason));client.DefaultRequestHeaders.Add("X-Audit-Reason-Encoding","uri");
        var withdrawn=await client.PostAsJsonAsync($"/api/approvals/removal/{removal.Id}/withdraw",new {reason});withdrawn.EnsureSuccessStatusCode();Assert.True(await db.Set<AuditEntry>().AnyAsync(a=>a.EntityId==removal.Id.ToString() && a.Reason==reason));
        using(var cancelled=JsonDocument.Parse(await client.GetStringAsync("/api/approvals/all?status=Cancelled"))){Assert.Equal(1,cancelled.RootElement.GetProperty("totalCount").GetInt32());Assert.Equal("removal",cancelled.RootElement.GetProperty("items")[0].GetProperty("kind").GetString());}
        foreach(var query in new[]{"status=Unknown","page=0","pageSize=101","page=2147483647","requestId=00000000-0000-0000-0000-000000000000"})Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/approvals/all?"+query)).StatusCode);
    }
}
