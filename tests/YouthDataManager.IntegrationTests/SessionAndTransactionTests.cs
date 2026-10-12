using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Abstractions.DTOs;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Students.Service.Abstractions.DTOs;
using YouthDataManager.Students.Service.Implementations.Commands;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class SessionAndTransactionTests(SqlFixture fixture)
{
    [Fact]
    public async Task Refresh_tokens_are_hashed_rotated_and_replay_revokes_descendants()
    {
        await using var scope=fixture.Provider.CreateAsyncScope(); var p=scope.ServiceProvider;
        var user=await fixture.User(p); var auth=p.GetRequiredService<IAuthCommandsService>();
        var login=(await auth.Login(new(user.UserName!,"password123"))).Data!;
        var db=p.GetRequiredService<AppDbContext>();
        Assert.False(await db.Set<RefreshSession>().AnyAsync(s => s.TokenHash==login.RefreshToken));
        var replacement=(await auth.RefreshToken(new(login.Token,login.RefreshToken))).Data!;
        Assert.NotEqual(login.RefreshToken,replacement.RefreshToken);
        Assert.Equal(ServiceResultStatus.Failure,(await auth.RefreshToken(new(login.Token,login.RefreshToken))).Status);
        Assert.Equal(ServiceResultStatus.Failure,(await auth.RefreshToken(new(replacement.Token,replacement.RefreshToken))).Status);
    }
    [Fact]
    public async Task Failure_rolls_back_business_and_audit_and_does_not_poison_next_operation()
    {
        await using var scope=fixture.Provider.CreateAsyncScope(); var p=scope.ServiceProvider;
        var store=p.GetRequiredService<IApplicationStore>(); var id=Guid.NewGuid();
        await store.Transaction(async () => { store.Add(new Student { Id=id,FullName="Rollback",NormalizedFullName="rollback",Phone="01012345678" }); await store.Save(); return false; }, success => success);
        Assert.False(await store.Query<Student>(false).AnyAsync(s => s.Id==id));
        Assert.False(await store.Query<AuditEntry>(false).AnyAsync(s => s.EntityId==id.ToString()));
        await store.Transaction(() => { store.Add(new Student { Id=id,FullName="Valid",NormalizedFullName="valid",Phone="01012345678" }); return Task.FromResult(true); });
        Assert.True(await store.Query<Student>(false).AnyAsync(s => s.Id==id));
    }
    [Fact]
    public async Task Ordinary_password_change_revokes_refresh_and_recalculates_onboarding()
    {
        await using var scope=fixture.Provider.CreateAsyncScope(); var p=scope.ServiceProvider;
        var user=await fixture.User(p); var auth=p.GetRequiredService<IAuthCommandsService>();
        var login=(await auth.Login(new(user.UserName!,"password123"))).Data!;
        var changed=await auth.ChangePassword(user.Id,new("password123","replacement123"));
        Assert.Equal(ServiceResultStatus.Success,changed.Status); Assert.False(changed.Data!.RequiresPasswordChange);
        Assert.Equal(ServiceResultStatus.Failure,(await auth.RefreshToken(new(login.Token,login.RefreshToken))).Status);
    }
    [Fact]
    public async Task Student_update_preserves_omitted_fields_and_clears_explicit_null()
    {
        await using var scope=fixture.Provider.CreateAsyncScope(); var p=scope.ServiceProvider;
        var store=p.GetRequiredService<IApplicationStore>(); var caller=p.GetRequiredService<TestCaller>();
        var owner=await fixture.User(p); var id=Guid.NewGuid();
        store.Add(new Student { Id=id,FullName="Test",NormalizedFullName="test",Phone="01012345678",SecondaryPhone="01112345678",BirthMonth=6,ServantId=owner.Id }); await store.Save();
        caller.UserId=owner.Id; caller.IsPrivileged=false;
        var service=new StudentCommandsService(store,caller);
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var omitted=JsonSerializer.Deserialize<UpdateStudentRequest>($"{{\"id\":\"{id}\",\"fullName\":\"Test\",\"phone\":\"01012345678\",\"gender\":0}}",options)!;
        Assert.False(omitted.BirthDate.IsPresent);
        Assert.Equal(ServiceResultStatus.Success,(await service.Update(omitted,owner.Id,false)).Status);
        var s=await store.Query<Student>().SingleAsync(s=>s.Id==id); Assert.Equal(6,s.BirthMonth); Assert.Equal("01112345678",s.SecondaryPhone);
        var clear=JsonSerializer.Deserialize<UpdateStudentRequest>($"{{\"id\":\"{id}\",\"fullName\":\"Test\",\"phone\":\"01012345678\",\"gender\":0,\"secondaryPhone\":null}}",options)!;
        Assert.True(clear.SecondaryPhone.IsPresent); Assert.Equal(ServiceResultStatus.Success,(await service.Update(clear,owner.Id,false)).Status); Assert.Null(s.SecondaryPhone);
    }
}
