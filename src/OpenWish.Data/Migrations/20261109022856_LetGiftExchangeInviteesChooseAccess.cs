using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class LetGiftExchangeInviteesChooseAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Invitees now choose between an account and private no-account access.
            migrationBuilder.Sql(
                "UPDATE \"Events\" SET \"AllowGuestParticipants\" = TRUE WHERE \"IsGiftExchange\" AND NOT \"AllowGuestParticipants\" AND NOT \"Deleted\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only change; earlier per-event choices cannot be restored.
        }
    }
}
