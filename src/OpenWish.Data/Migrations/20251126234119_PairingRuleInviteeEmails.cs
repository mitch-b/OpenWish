using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class PairingRuleInviteeEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TargetUserId",
                table: "CustomPairingRules",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SourceUserId",
                table: "CustomPairingRules",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "SourceInviteeEmail",
                table: "CustomPairingRules",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetInviteeEmail",
                table: "CustomPairingRules",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceInviteeEmail",
                table: "CustomPairingRules");

            migrationBuilder.DropColumn(
                name: "TargetInviteeEmail",
                table: "CustomPairingRules");

            migrationBuilder.AlterColumn<string>(
                name: "TargetUserId",
                table: "CustomPairingRules",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceUserId",
                table: "CustomPairingRules",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
