using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemediationFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {







            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "Students",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentRemovalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "StudentRemovalRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentAssignmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "StudentAssignmentRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "BirthMonth",
                schema: "dbo",
                table: "StudentAdditionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedStudentId",
                schema: "dbo",
                table: "StudentAdditionRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentAdditionRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                schema: "dbo",
                table: "StudentAdditionRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                schema: "dbo",
                table: "HomeVisits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                schema: "dbo",
                table: "HomeVisits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                schema: "dbo",
                table: "CallLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                schema: "dbo",
                table: "CallLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditEntry",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FollowUpTask",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowUpTask", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRowOutcome",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Row = table.Column<int>(type: "int", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRowOutcome", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingRosterEntry",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedServantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Present = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRosterEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeetingSession",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RosterKnown = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDelivery",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    AvailableAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClaimedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDelivery", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshSession",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshSession", x => x.Id);
                });







            migrationBuilder.CreateIndex(
                name: "IX_AuditEntry_EntityType_EntityId_Timestamp",
                schema: "dbo",
                table: "AuditEntry",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTask_AssigneeId_Status_DueAt",
                schema: "dbo",
                table: "FollowUpTask",
                columns: new[] { "AssigneeId", "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FollowUpTask_SourceType_SourceId",
                schema: "dbo",
                table: "FollowUpTask",
                columns: new[] { "SourceType", "SourceId" },
                unique: true,
                filter: "[SourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRowOutcome_OperationId_ActorId_Row",
                schema: "dbo",
                table: "ImportRowOutcome",
                columns: new[] { "OperationId", "ActorId", "Row" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRosterEntry_SessionId_Kind_PersonId",
                schema: "dbo",
                table: "MeetingRosterEntry",
                columns: new[] { "SessionId", "Kind", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingSession_Date",
                schema: "dbo",
                table: "MeetingSession",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDelivery_DeliveredAt_AvailableAt",
                schema: "dbo",
                table: "NotificationDelivery",
                columns: new[] { "DeliveredAt", "AvailableAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDelivery_OccurrenceKey",
                schema: "dbo",
                table: "NotificationDelivery",
                column: "OccurrenceKey",
                unique: true,
                filter: "[OccurrenceKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSession_TokenHash",
                schema: "dbo",
                table: "RefreshSession",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshSession_UserId",
                schema: "dbo",
                table: "RefreshSession",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "FollowUpTask",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ImportRowOutcome",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MeetingRosterEntry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MeetingSession",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "NotificationDelivery",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RefreshSession",
                schema: "dbo");







            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentRemovalRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "StudentRemovalRequests");

            migrationBuilder.DropColumn(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentAssignmentRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "StudentAssignmentRequests");

            migrationBuilder.DropColumn(
                name: "BirthMonth",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "CreatedStudentId",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "ProcessingReason",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "StudentAdditionRequests");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                schema: "dbo",
                table: "HomeVisits");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                schema: "dbo",
                table: "HomeVisits");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                schema: "dbo",
                table: "CallLogs");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                schema: "dbo",
                table: "CallLogs");







        }
    }
}
