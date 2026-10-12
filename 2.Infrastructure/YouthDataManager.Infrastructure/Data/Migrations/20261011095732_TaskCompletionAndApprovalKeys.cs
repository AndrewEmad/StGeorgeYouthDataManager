using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaskCompletionAndApprovalKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestKey",
                schema: "dbo",
                table: "StudentAdditionRequests",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedRecordId",
                schema: "dbo",
                table: "FollowUpTask",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAdditionRequests_RequestedByUserId_RequestKey",
                schema: "dbo",
                table: "StudentAdditionRequests",
                columns: new[] { "RequestedByUserId", "RequestKey" },
                unique: true,
                filter: "[Status] = 'Pending' AND [RequestKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentAdditionRequests_RequestedByUserId_RequestKey",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "RequestKey",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "CompletedRecordId",
                schema: "dbo",
                table: "FollowUpTask");
        }
    }
}
