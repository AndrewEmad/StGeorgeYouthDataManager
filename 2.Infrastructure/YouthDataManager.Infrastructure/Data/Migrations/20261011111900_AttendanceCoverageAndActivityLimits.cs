using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceCoverageAndActivityLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "dbo",
                table: "HomeVisits",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "dbo",
                table: "CallLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRosterEntry_Kind_PersonId_SessionId",
                schema: "dbo",
                table: "MeetingRosterEntry",
                columns: new[] { "Kind", "PersonId", "SessionId" })
                .Annotation("SqlServer:Include", new[] { "Present", "AssignedServantId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingRosterEntry_Kind_PersonId_SessionId",
                schema: "dbo",
                table: "MeetingRosterEntry");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "dbo",
                table: "HomeVisits",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "dbo",
                table: "CallLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }
    }
}
