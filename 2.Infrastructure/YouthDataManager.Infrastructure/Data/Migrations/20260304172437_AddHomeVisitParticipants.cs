using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeVisitParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeVisitParticipants",
                schema: "dbo",
                columns: table => new
                {
                    HomeVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeVisitParticipants", x => new { x.HomeVisitId, x.ServantId });
                    table.ForeignKey(
                        name: "FK_HomeVisitParticipants_AspNetUsers_ServantId",
                        column: x => x.ServantId,
                        principalSchema: "dbo",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HomeVisitParticipants_HomeVisits_HomeVisitId",
                        column: x => x.HomeVisitId,
                        principalSchema: "dbo",
                        principalTable: "HomeVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HomeVisitParticipants_ServantId",
                schema: "dbo",
                table: "HomeVisitParticipants",
                column: "ServantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeVisitParticipants",
                schema: "dbo");
        }
    }
}
