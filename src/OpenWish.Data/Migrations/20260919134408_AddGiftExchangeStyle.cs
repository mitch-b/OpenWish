using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGiftExchangeStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GiftExchangeStyle",
                table: "Events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Events"
                SET "GiftExchangeStyle" = CASE
                    WHEN "IsGiftExchange" THEN 'SecretSanta'
                    ELSE 'GiftExchange'
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "GiftExchangeStyle",
                table: "Events",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "GiftExchange",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiftExchangeStyle",
                table: "Events");
        }
    }
}
