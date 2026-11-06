using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWish.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectedApps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "WishlistItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceAppName",
                table: "WishlistItems",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceConnectionId",
                table: "WishlistItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IntegrationAuthorizationCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RedirectUri = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CodeChallenge = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GrantedWishlistIds = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    DefaultWishlistId = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationAuthorizationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationAuthorizationCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AppName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AccessTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TokenIssuedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastUsedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DefaultWishlistId = table.Column<int>(type: "integer", nullable: true),
                    PublicId = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Wishlists_DefaultWishlistId",
                        column: x => x.DefaultWishlistId,
                        principalTable: "Wishlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnectionWishlists",
                columns: table => new
                {
                    IntegrationConnectionId = table.Column<int>(type: "integer", nullable: false),
                    WishlistId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnectionWishlists", x => new { x.IntegrationConnectionId, x.WishlistId });
                    table.ForeignKey(
                        name: "FK_IntegrationConnectionWishlists_IntegrationConnections_Integ~",
                        column: x => x.IntegrationConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationConnectionWishlists_Wishlists_WishlistId",
                        column: x => x.WishlistId,
                        principalTable: "Wishlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_SourceConnectionId_ExternalId",
                table: "WishlistItems",
                columns: new[] { "SourceConnectionId", "ExternalId" },
                unique: true,
                filter: "\"Deleted\" = false AND \"SourceConnectionId\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuthorizationCodes_CodeHash",
                table: "IntegrationAuthorizationCodes",
                column: "CodeHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuthorizationCodes_UserId",
                table: "IntegrationAuthorizationCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_AccessTokenHash",
                table: "IntegrationConnections",
                column: "AccessTokenHash",
                unique: true,
                filter: "\"AccessTokenHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_DefaultWishlistId",
                table: "IntegrationConnections",
                column: "DefaultWishlistId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_PublicId",
                table: "IntegrationConnections",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_UserId_ClientId",
                table: "IntegrationConnections",
                columns: new[] { "UserId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnectionWishlists_WishlistId",
                table: "IntegrationConnectionWishlists",
                column: "WishlistId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishlistItems_IntegrationConnections_SourceConnectionId",
                table: "WishlistItems",
                column: "SourceConnectionId",
                principalTable: "IntegrationConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WishlistItems_IntegrationConnections_SourceConnectionId",
                table: "WishlistItems");

            migrationBuilder.DropTable(
                name: "IntegrationAuthorizationCodes");

            migrationBuilder.DropTable(
                name: "IntegrationConnectionWishlists");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_SourceConnectionId_ExternalId",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "SourceAppName",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "SourceConnectionId",
                table: "WishlistItems");
        }
    }
}
