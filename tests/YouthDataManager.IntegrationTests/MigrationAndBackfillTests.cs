using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using YouthDataManager.Domain.Entities;
using YouthDataManager.Domain.Enums;
using YouthDataManager.Infrastructure.Data;
using YouthDataManager.Shared.Service.Abstractions;

namespace YouthDataManager.IntegrationTests;
[Collection("SQL")]
public sealed class MigrationAndBackfillTests(SqlFixture fixture)
{
    [Fact]
    public async Task Legacy_upgrade_is_additive_constraints_stop_without_truncation_and_backfills_are_idempotent()
    {
        var connection=new SqlConnectionStringBuilder(fixture.Connection){InitialCatalog="YDM_Upgrade_"+Guid.NewGuid().ToString("N")};
        await using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options,new TestCaller());
        try {
            var migrator=db.GetService<IMigrator>();await migrator.MigrateAsync("20260304172437_AddHomeVisitParticipants");
            var id=Guid.NewGuid();var oversized=new string('1',35);
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.Students(Id,FullName,NormalizedFullName,Address,Area,Phone,SecondaryPhone,BirthDate,BirthMonth,College,AcademicYear,ConfessionFather,Notes,Gender,FollowUpAreas,OrderIndex,CreatedAt,IsDeleted) VALUES({id},N'Legacy',N'legacy',N'Cairo',N'Test',N'01012345678',{oversized},{new DateTime(2000,6,3)},1,N'College',N'Year',N'Father',N'Notes',0,N'',0,{new DateTime(2020,1,1)},0)");
            await migrator.MigrateAsync("20261011115500_MeasuredAttendanceOwnershipIndex");
            Assert.Equal(oversized,(await db.Students.SingleAsync(s=>s.Id==id)).SecondaryPhone);
            var error=await Assert.ThrowsAsync<SqlException>(()=>migrator.MigrateAsync());Assert.Equal(51002,error.Number);
            Assert.DoesNotContain("20261011120000_ReviewedLegacyConstraints",await db.Database.GetAppliedMigrationsAsync());
            db.ChangeTracker.Clear();var legacy=await db.Students.SingleAsync(s=>s.Id==id);legacy.SecondaryPhone="+44 (7700).900123";await db.SaveChangesAsync();
            await migrator.MigrateAsync();db.ChangeTracker.Clear();legacy=await db.Students.SingleAsync(s=>s.Id==id);
            Assert.Equal(PhoneFields.Canonical(legacy.SecondaryPhone),legacy.NormalizedSecondaryPhone);
            var owner=new ApplicationUser {Id=Guid.NewGuid(),UserName="backfill",FullName="Backfill",Phone="01012345678",Address="Cairo"};db.Users.Add(owner);await db.SaveChangesAsync();
            var at=new DateTime(2020,2,3,8,9,10,DateTimeKind.Utc);var call=new CallLog {Id=Guid.NewGuid(),StudentId=id,ServantId=owner.Id,CallDate=at,CallStatus=CallStatus.NotAnswered,NextFollowUpDate=at.AddDays(7)};db.CallLogs.Add(call);await db.SaveChangesAsync();
            var backfill=new LegacyBackfill(new ApplicationStore(db));await backfill.Run(false,100,default);await backfill.Run(false,100,default);
            Assert.Equal(1,await db.Set<FollowUpTask>().CountAsync(t=>t.SourceType=="CallLog" && t.SourceId==call.Id && t.Status=="PendingReview"));
            Assert.Equal(6,(await db.Students.SingleAsync(s=>s.Id==id)).BirthMonth);Assert.Equal(at,(await db.CallLogs.SingleAsync(c=>c.Id==call.Id)).CallDate);
        } finally { await db.Database.EnsureDeletedAsync(); }
    }
}
