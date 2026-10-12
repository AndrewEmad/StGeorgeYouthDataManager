using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace YouthDataManager.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011115500_MeasuredAttendanceOwnershipIndex")]
public sealed class MeasuredAttendanceOwnershipIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        "CREATE INDEX IX_MeetingRosterEntry_AssignedServantId_Kind_PersonId_SessionId ON dbo.MeetingRosterEntry (AssignedServantId,Kind,PersonId,SessionId) INCLUDE (Present)");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(
        "DROP INDEX IF EXISTS IX_MeetingRosterEntry_AssignedServantId_Kind_PersonId_SessionId ON dbo.MeetingRosterEntry");
}
