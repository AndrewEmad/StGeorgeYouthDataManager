using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Infrastructure.Data;

namespace YouthDataManager.IntegrationTests;

[Collection("SQL")]
public sealed class StartupMigrationTests(SqlFixture fixture)
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Startup_migrates_a_fresh_database_by_default_and_restarts_without_duplicate_data(string environment)
    {
        var connection = new SqlConnectionStringBuilder(fixture.Connection)
        {
            InitialCatalog = "YDM_Startup_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options, new TestCaller());
        try
        {
            Assert.False(await db.Database.CanConnectAsync());
            await using (var factory = new IsolatedApiFactory(connection, environment, initializeOnStartup: null))
            {
                using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
            }

            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(5, await db.Roles.CountAsync());
            var admin = await db.Users.SingleAsync(u => u.Email == "admin@youthdatamanager.com");
            Assert.True(await db.UserRoles.AnyAsync(r => r.UserId == admin.Id &&
                db.Roles.Any(role => role.Id == r.RoleId && role.Name == "Admin")));

            admin.FullName = "Existing administrator profile";
            var student = new Student
            {
                Id = Guid.NewGuid(), FullName = "Preserved across restart",
                NormalizedFullName = "preserved across restart", Phone = "01012345678"
            };
            db.Add(student);
            await db.SaveChangesAsync();

            await using (var factory = new IsolatedApiFactory(connection, environment, initializeOnStartup: null))
            {
                using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
            }

            db.ChangeTracker.Clear();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(5, await db.Roles.CountAsync());
            Assert.Equal(1, await db.Users.CountAsync());
            Assert.Equal("Existing administrator profile", (await db.Users.SingleAsync()).FullName);
            Assert.True(await db.Students.AnyAsync(s => s.Id == student.Id));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Explicit_opt_out_keeps_an_isolated_test_database_uninitialized()
    {
        var connection = new SqlConnectionStringBuilder(fixture.Connection)
        {
            InitialCatalog = "YDM_StartupDisabled_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options, new TestCaller());
        try
        {
            await using var factory = new IsolatedApiFactory(connection, "Production", initializeOnStartup: false);
            using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
            Assert.False(await db.Database.CanConnectAsync());
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
