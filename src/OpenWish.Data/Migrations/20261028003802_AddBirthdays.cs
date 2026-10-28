using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBirthdays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BirthdayDay",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthdayMonth",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthdayDay",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BirthdayMonth",
                table: "AspNetUsers");
        }
    }
}
