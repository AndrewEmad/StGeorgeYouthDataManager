using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttendanceDate",
                schema: "dbo",
                table: "Students",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServantAttendances",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServantAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServantAttendances_AspNetUsers_ServantId",
                        column: x => x.ServantId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAttendances",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAttendances_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServantAttendances_ServantId_Date",
                schema: "dbo",
                table: "ServantAttendances",
                columns: new[] { "ServantId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAttendances_StudentId_Date",
                schema: "dbo",
                table: "StudentAttendances",
                columns: new[] { "StudentId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServantAttendances",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "StudentAttendances",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "LastAttendanceDate",
                schema: "dbo",
                table: "Students");
        }
    }
}
