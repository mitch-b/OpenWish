using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventInvitationFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EventUsers",
                table: "EventUsers");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "EventUsers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "EventUsers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "InviteeEmail",
                table: "EventUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EventUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventUsers",
                table: "EventUsers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_EventId",
                table: "EventUsers",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_EventUsers",
                table: "EventUsers");

            migrationBuilder.DropIndex(
                name: "IX_EventUsers_EventId",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "InviteeEmail",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EventUsers");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "EventUsers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "EventUsers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventUsers",
                table: "EventUsers",
                columns: new[] { "EventId", "UserId" });
        }
    }
}
