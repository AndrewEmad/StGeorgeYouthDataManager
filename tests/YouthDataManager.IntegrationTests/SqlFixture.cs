using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions;
using YouthDataManager.Auth.Service.Abstractions.Commands;
using YouthDataManager.Auth.Service.Implementations.Commands;
using YouthDataManager.Auth.Service.Implementations.Services;

namespace YouthDataManager.IntegrationTests;

public sealed class TestCaller : ICallerContext
{
    public Guid? UserId { get; set; }
    public bool IsPrivileged { get; set; } = true;
    public CancellationToken CancellationToken { get; set; }
    public string CorrelationId => "integration-test";
    public string? Reason => "Regression verification";
}
public sealed class SqlFixture : IAsyncLifetime
{
    public ServiceProvider Provider { get; private set; } = null!;
    public string Connection { get; } = Environment.GetEnvironmentVariable("YDM_TEST_SQL_SERVER") is { Length: > 0 } server
        ? $"{server};Database=YDM_Test_{Guid.NewGuid():N};MultipleActiveResultSets=false"
        : $"Server=(localdb)\\MSSQLLocalDB;Database=YDM_Test_{Guid.NewGuid():N};Integrated Security=true;MultipleActiveResultSets=false";
    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddDataProtection();
        services.AddScoped<TestCaller>(); services.AddScoped<ICallerContext>(p => p.GetRequiredService<TestCaller>());
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(Connection));
        services.AddScoped<IApplicationStore,ApplicationStore>();
        services.AddIdentityCore<ApplicationUser>(o => { o.Password.RequireNonAlphanumeric=false; o.Password.RequireUppercase=false; o.Password.RequireDigit=false; o.Lockout.MaxFailedAccessAttempts=5; })
            .AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        services.Configure<JwtConfiguration>(o => { o.Secret="isolated-integration-secret-of-at-least-32-bytes"; o.ValidIssuer="test"; o.ValidAudience="test"; });
        services.AddScoped<IJwtTokenService,JwtTokenService>(); services.AddScoped<IAuthCommandsService,AuthCommandsService>();
        Provider=services.BuildServiceProvider();
        await using var scope=Provider.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var name in new[] {"Admin","Manager","Priest","Secretary","Servant"}) Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(name))).Succeeded);
    }
    public async Task DisposeAsync()
    {
        await using var scope=Provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        await Provider.DisposeAsync();
    }
    public async Task<ApplicationUser> User(IServiceProvider services, string role="Servant")
    {
        var manager=services.GetRequiredService<UserManager<ApplicationUser>>();
        var user=new ApplicationUser { Id=Guid.NewGuid(), UserName=$"test_{Guid.NewGuid():N}", FullName="اسم متكرر", Phone="01012345678", Address="القاهرة", MustChangePassword=false };
        Assert.True((await manager.CreateAsync(user,"password123")).Succeeded);
        Assert.True((await manager.AddToRoleAsync(user,role)).Succeeded);
        return user;
    }
}
[CollectionDefinition("SQL")]
public class SqlCollection : ICollectionFixture<SqlFixture>;
