using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YouthDataManager.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhoneFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneFollowUp",
                schema: "dbo",
                table: "Students");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneFollowUp",
                schema: "dbo",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
