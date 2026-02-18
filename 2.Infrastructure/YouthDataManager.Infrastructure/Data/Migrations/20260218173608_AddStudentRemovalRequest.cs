using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRemovalRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentRemovalRequests",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRemovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRemovalRequests_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRemovalRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRemovalRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "dbo",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_ProcessedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_Status",
                schema: "dbo",
                table: "StudentRemovalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRemovalRequests_StudentId_RequestedByUserId",
                schema: "dbo",
                table: "StudentRemovalRequests",
                columns: new[] { "StudentId", "RequestedByUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentRemovalRequests",
                schema: "dbo");
        }
    }
}
