using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Domain.Utilities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class ActivityHistoryTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("Manager")]
    [InlineData("Admin")]
    [InlineData("Priest")]
    [InlineData("Secretary")]
    [InlineData("Servant")]
    public async Task Personal_history_is_scoped_for_every_role_and_filters_count_the_same_rows(string role)
    {
        await using var scope=fixture.Provider.CreateAsyncScope();var services=scope.ServiceProvider;
        var actor=await fixture.User(services,role);var other=await fixture.User(services);
        var db=services.GetRequiredService<AppDbContext>();
        var student=new Student {Id=Guid.NewGuid(),FullName="History youth",NormalizedFullName="history youth",Phone="01012345678",ServantId=other.Id,IsDeleted=true};
        var day=new DateTime(2026,10,30);var start=BusinessTime.UtcStart(day);var end=BusinessTime.UtcStart(day.AddDays(1));
        var call=new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,CallDate=end.AddTicks(-1),CallStatus=CallStatus.Answered,Notes="matched"};
        var unrelated=new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=other.Id,CallDate=start.AddHours(1),CallStatus=CallStatus.Answered,Notes="other"};
        db.AddRange(student,call,unrelated,
            new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,CallDate=end,CallStatus=CallStatus.NotAnswered,Notes="outside"},
            new CallLog {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,CallDate=start.AddHours(1),CallStatus=CallStatus.Answered,Notes="voided",IsVoided=true,VoidReason="Correction"});
        var shared=new HomeVisit {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=other.Id,VisitDate=start.AddHours(2),VisitOutcome=VisitOutcome.Successful,Notes="matched"};
        var own=new HomeVisit {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=actor.Id,VisitDate=start.AddHours(3),VisitOutcome=VisitOutcome.Successful};
        db.AddRange(shared,own,new HomeVisit {Id=Guid.NewGuid(),StudentId=student.Id,ServantId=other.Id,VisitDate=start.AddHours(4)});
        db.AddRange(new HomeVisitParticipant {HomeVisitId=shared.Id,ServantId=actor.Id},new HomeVisitParticipant {HomeVisitId=own.Id,ServantId=actor.Id});
        await db.SaveChangesAsync();
        await using var factory=new IsolatedApiFactory(fixture.Connection);using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost")});
        var login=await client.PostAsJsonAsync("/api/Auth/login",new {username=actor.UserName,password="password123"});login.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization=new("Bearer",(await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token);
        using var calls=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/calls?pageSize=1"));
        Assert.Equal(3,calls.RootElement.GetProperty("totalCount").GetInt32());Assert.Single(calls.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(actor.Id,calls.RootElement.GetProperty("items")[0].GetProperty("recordedByServantId").GetGuid());
        using var visits=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/visits"));
        Assert.Equal(2,visits.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(new[]{shared.Id,own.Id}.Order(),visits.RootElement.GetProperty("items").EnumerateArray().Select(x=>x.GetProperty("id").GetGuid()).Order());
        using var filtered=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/calls?dateFrom=2026-10-30&dateTo=2026-10-30&outcome=1&status=Active&search=matched"));
        Assert.Equal(1,filtered.RootElement.GetProperty("totalCount").GetInt32());Assert.Equal(call.Id,filtered.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        using var filteredVisits=JsonDocument.Parse(await client.GetStringAsync("/api/activity-history/visits?dateFrom=2026-10-30&dateTo=2026-10-30&outcome=0&search=matched"));
        Assert.Equal(1,filteredVisits.RootElement.GetProperty("totalCount").GetInt32());Assert.Equal(shared.Id,filteredVisits.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid());
        var privileged=role is "Manager" or "Admin" or "Priest";
        using var hidden=JsonDocument.Parse(await client.GetStringAsync($"/api/activity-history/calls?recordId={unrelated.Id}"));
        Assert.Equal(0,hidden.RootElement.GetProperty("totalCount").GetInt32());
        var studentHistory=await client.GetAsync($"/api/activity-history/calls?studentId={student.Id}");
        Assert.Equal(privileged ? HttpStatusCode.OK : HttpStatusCode.Forbidden,studentHistory.StatusCode);
        if(privileged){using var historical=JsonDocument.Parse(await studentHistory.Content.ReadAsStringAsync());Assert.Equal(4,historical.RootElement.GetProperty("totalCount").GetInt32());}
        Assert.Equal(privileged ? HttpStatusCode.OK : HttpStatusCode.Forbidden,(await client.GetAsync($"/api/activity-history/calls?actorId={other.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/calls?dateFrom=2026-11-01&dateTo=2026-10-30")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/visits?outcome=4")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/calls?status=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/activity-history/calls?dateFrom=invalid")).StatusCode);
    }
}
