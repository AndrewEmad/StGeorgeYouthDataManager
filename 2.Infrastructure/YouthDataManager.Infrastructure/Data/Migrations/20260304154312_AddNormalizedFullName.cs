using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedFullName",
                schema: "dbo",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedFullName",
                schema: "dbo",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Students_NormalizedFullName",
                schema: "dbo",
                table: "Students",
                column: "NormalizedFullName");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedFullName",
                schema: "dbo",
                table: "AspNetUsers",
                column: "NormalizedFullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_NormalizedFullName",
                schema: "dbo",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedFullName",
                schema: "dbo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NormalizedFullName",
                schema: "dbo",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "NormalizedFullName",
                schema: "dbo",
                table: "AspNetUsers");
        }
    }
}
