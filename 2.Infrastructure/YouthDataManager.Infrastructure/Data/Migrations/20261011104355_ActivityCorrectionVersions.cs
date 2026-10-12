using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActivityCorrectionVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "HomeVisits",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "CallLogs",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "HomeVisits");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "CallLogs");
        }
    }
}
