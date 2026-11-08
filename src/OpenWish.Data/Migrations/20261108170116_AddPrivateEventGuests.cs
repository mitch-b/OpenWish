using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateEventGuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GiverParticipantId",
                table: "GiftExchanges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiverParticipantId",
                table: "GiftExchanges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GuestAccessExpiresOn",
                table: "EventUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestDisplayName",
                table: "EventUsers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GuestEmailSentOn",
                table: "EventUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestTokenHash",
                table: "EventUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowGuestParticipants",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_GiftExchanges_GiverParticipantId",
                table: "GiftExchanges",
                column: "GiverParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftExchanges_ReceiverParticipantId",
                table: "GiftExchanges",
                column: "ReceiverParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_EventUsers_GuestTokenHash",
                table: "EventUsers",
                column: "GuestTokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GiftExchanges_EventUsers_GiverParticipantId",
                table: "GiftExchanges",
                column: "GiverParticipantId",
                principalTable: "EventUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GiftExchanges_EventUsers_ReceiverParticipantId",
                table: "GiftExchanges",
                column: "ReceiverParticipantId",
                principalTable: "EventUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiftExchanges_EventUsers_GiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropForeignKey(
                name: "FK_GiftExchanges_EventUsers_ReceiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropIndex(
                name: "IX_GiftExchanges_GiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropIndex(
                name: "IX_GiftExchanges_ReceiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropIndex(
                name: "IX_EventUsers_GuestTokenHash",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "GiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropColumn(
                name: "ReceiverParticipantId",
                table: "GiftExchanges");

            migrationBuilder.DropColumn(
                name: "GuestAccessExpiresOn",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "GuestDisplayName",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "GuestEmailSentOn",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "GuestTokenHash",
                table: "EventUsers");

            migrationBuilder.DropColumn(
                name: "AllowGuestParticipants",
                table: "Events");
        }
    }
}
