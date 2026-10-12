using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DurableNotificationDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClaimId",
                schema: "dbo",
                table: "NotificationDelivery",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "NotificationDelivery",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NotificationDeviceDelivery",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PermanentFailure = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeviceDelivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkerCheckpoint",
                schema: "dbo",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompletedThrough = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerCheckpoint", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeviceDelivery_DeliveryId_DeviceId",
                schema: "dbo",
                table: "NotificationDeviceDelivery",
                columns: new[] { "DeliveryId", "DeviceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeviceDelivery",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WorkerCheckpoint",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                schema: "dbo",
                table: "NotificationDelivery");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "dbo",
                table: "NotificationDelivery");
        }
    }
}
