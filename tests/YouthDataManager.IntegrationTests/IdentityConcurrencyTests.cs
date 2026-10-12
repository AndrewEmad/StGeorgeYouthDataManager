using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Shared.Service.Implementations;
using YouthDataManager.Users.Service.Implementations;
using YouthDataManager.Users.Service.Abstractions.DTOs;
namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class IdentityConcurrencyTests(SqlFixture fixture) {
 private static UserService Service(IServiceProvider p)=>new(p.GetRequiredService<UserManager<ApplicationUser>>(),p.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),new ActivityLogger(p.GetRequiredService<IApplicationStore>(),new HttpContextAccessor()),p.GetRequiredService<IApplicationStore>());
 [Fact] public async Task Concurrent_deactivation_cannot_remove_both_last_active_Admins(){
  await using var seed=fixture.Provider.CreateAsyncScope();var a=await fixture.User(seed.ServiceProvider,"Admin");var b=await fixture.User(seed.ServiceProvider,"Admin");
  await using var left=fixture.Provider.CreateAsyncScope();await using var right=fixture.Provider.CreateAsyncScope();
  var results=await Task.WhenAll(Service(left.ServiceProvider).ToggleStatusAsync(a.Id),Service(right.ServiceProvider).ToggleStatusAsync(b.Id));
  Assert.Single(results.Where(r=>r.Status==ServiceResultStatus.Success));
  await using var verify=fixture.Provider.CreateAsyncScope();var manager=verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();Assert.Single((await manager.GetUsersInRoleAsync("Admin")).Where(u=>u.IsActive));
  // These accounts remain historical records; remove the test-only role after verification.
  foreach(var user in await manager.GetUsersInRoleAsync("Admin"))Assert.True((await manager.RemoveFromRoleAsync(user,"Admin")).Succeeded);
 }
 [Fact] public async Task Concurrent_Priest_creation_allows_one_and_rolls_back_the_loser(){
  await using var left=fixture.Provider.CreateAsyncScope();await using var right=fixture.Provider.CreateAsyncScope();
  var prefix="priest_"+Guid.NewGuid().ToString("N");
  CreateUserDto Dto(string suffix)=>new(prefix+suffix,"Same name","","01012345678","password123","Priest");
  var results=await Task.WhenAll(Service(left.ServiceProvider).CreateAsync(Dto("a")),Service(right.ServiceProvider).CreateAsync(Dto("b")));
  Assert.Single(results.Where(r=>r.Status==ServiceResultStatus.Success));
  await using var verify=fixture.Provider.CreateAsyncScope();var db=verify.ServiceProvider.GetRequiredService<AppDbContext>();Assert.Equal(1,await db.Users.CountAsync(u=>u.UserName!.StartsWith(prefix)));
  var manager=verify.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var priest=Assert.Single(await manager.GetUsersInRoleAsync("Priest"));Assert.True((await manager.RemoveFromRoleAsync(priest,"Priest")).Succeeded);
 }
}
