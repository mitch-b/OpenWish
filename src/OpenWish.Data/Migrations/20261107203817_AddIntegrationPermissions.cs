using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowAllWishlists",
                table: "IntegrationConnections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManageExistingItems",
                table: "IntegrationConnections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowAllWishlists",
                table: "IntegrationAuthorizationCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManageExistingItems",
                table: "IntegrationAuthorizationCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowAllWishlists",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "ManageExistingItems",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "AllowAllWishlists",
                table: "IntegrationAuthorizationCodes");

            migrationBuilder.DropColumn(
                name: "ManageExistingItems",
                table: "IntegrationAuthorizationCodes");
        }
    }
}
