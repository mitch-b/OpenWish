using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccessTokenExpiresOn",
                table: "IntegrationConnections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Audience",
                table: "IntegrationConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreviousRefreshTokenHash",
                table: "IntegrationConnections",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenHash",
                table: "IntegrationConnections",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Audience",
                table: "IntegrationAuthorizationCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IntegrationClientRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ClientUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    RedirectUris = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUsedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationClientRegistrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_PreviousRefreshTokenHash",
                table: "IntegrationConnections",
                column: "PreviousRefreshTokenHash",
                filter: "\"PreviousRefreshTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_RefreshTokenHash",
                table: "IntegrationConnections",
                column: "RefreshTokenHash",
                unique: true,
                filter: "\"RefreshTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationClientRegistrations_ClientId",
                table: "IntegrationClientRegistrations",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationClientRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationConnections_PreviousRefreshTokenHash",
                table: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationConnections_RefreshTokenHash",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "AccessTokenExpiresOn",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "PreviousRefreshTokenHash",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "RefreshTokenHash",
                table: "IntegrationConnections");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "IntegrationAuthorizationCodes");
        }
    }
}
