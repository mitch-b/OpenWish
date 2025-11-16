using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Wishlists",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "WishlistReactions",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "WishlistPermissions",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "WishlistItems",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "WishlistComments",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "WillPurchases",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "PublicWishlists",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Notifications",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ItemReservations",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ItemReactions",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ItemComments",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "GiftExchanges",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Friends",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "FriendRequests",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "EventUsers",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "CustomPairingRules",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "ActivityLogs",
                type: "text",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_PublicId",
                table: "Wishlists",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_PublicId",
                table: "Events",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wishlists_PublicId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Events_PublicId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "WishlistReactions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "WishlistPermissions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "WishlistComments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "WillPurchases");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "PublicWishlists");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ItemReservations");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ItemReactions");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ItemComments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "GiftExchanges");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Friends");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "FriendRequests");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "CustomPairingRules");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ActivityLogs");
        }
    }
}
