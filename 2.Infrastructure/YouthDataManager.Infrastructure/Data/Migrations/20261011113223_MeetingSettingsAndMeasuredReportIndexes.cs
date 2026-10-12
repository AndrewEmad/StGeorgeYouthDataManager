using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MeetingSettingsAndMeasuredReportIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.HomeVisits') AND name='IX_HomeVisits_StudentId_VisitDate') DROP INDEX IX_HomeVisits_StudentId_VisitDate ON dbo.HomeVisits;");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CallLogs') AND name='IX_CallLogs_StudentId_CallDate') DROP INDEX IX_CallLogs_StudentId_CallDate ON dbo.CallLogs;");

            migrationBuilder.CreateTable(
                name: "MeetingConfiguration",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    MeetingDay = table.Column<int>(type: "int", nullable: false),
                    ReportWeekStart = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingConfiguration", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeVisits_StudentId_VisitDate",
                schema: "dbo",
                table: "HomeVisits",
                columns: new[] { "StudentId", "VisitDate" })
                .Annotation("SqlServer:Include", new[] { "IsVoided", "VisitOutcome" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_StudentId_CallDate",
                schema: "dbo",
                table: "CallLogs",
                columns: new[] { "StudentId", "CallDate" })
                .Annotation("SqlServer:Include", new[] { "IsVoided", "CallStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingConfiguration",
                schema: "dbo");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.HomeVisits') AND name='IX_HomeVisits_StudentId_VisitDate') DROP INDEX IX_HomeVisits_StudentId_VisitDate ON dbo.HomeVisits;");

            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.CallLogs') AND name='IX_CallLogs_StudentId_CallDate') DROP INDEX IX_CallLogs_StudentId_CallDate ON dbo.CallLogs;");
        }
    }
}
