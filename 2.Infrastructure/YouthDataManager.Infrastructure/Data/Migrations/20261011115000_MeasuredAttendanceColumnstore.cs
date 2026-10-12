using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace YouthDataManager.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261011115000_MeasuredAttendanceColumnstore")]
public sealed class MeasuredAttendanceColumnstore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Measured against 317,200 roster rows and 20 concurrent callers. Keep
        // the rowstore indexes for recording and bounded roster lookups.
        migrationBuilder.Sql("CREATE NONCLUSTERED COLUMNSTORE INDEX IX_MeetingRosterEntry_ReportColumnstore ON dbo.MeetingRosterEntry (Kind,PersonId,SessionId,Present,AssignedServantId)");
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_MeetingRosterEntry_ReportColumnstore ON dbo.MeetingRosterEntry");
}
