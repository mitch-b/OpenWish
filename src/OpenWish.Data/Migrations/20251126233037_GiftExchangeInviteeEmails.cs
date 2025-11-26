using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class GiftExchangeInviteeEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReceiverId",
                table: "GiftExchanges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "GiverId",
                table: "GiftExchanges",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "GiverEmail",
                table: "GiftExchanges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverEmail",
                table: "GiftExchanges",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiverEmail",
                table: "GiftExchanges");

            migrationBuilder.DropColumn(
                name: "ReceiverEmail",
                table: "GiftExchanges");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverId",
                table: "GiftExchanges",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GiverId",
                table: "GiftExchanges",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
