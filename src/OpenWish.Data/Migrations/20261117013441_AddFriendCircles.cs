using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OpenWish.Data.Migrations;

/// <inheritdoc />
public partial class AddFriendCircles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CircleId",
            table: "WishlistPermissions",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "Circles",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                PublicId = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()"),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Circles", x => x.Id);
                table.ForeignKey(
                    name: "FK_Circles_AspNetUsers_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CircleMembers",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CircleId = table.Column<int>(type: "integer", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                PublicId = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()"),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CircleMembers", x => x.Id);
                table.ForeignKey(
                    name: "FK_CircleMembers_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CircleMembers_Circles_CircleId",
                    column: x => x.CircleId,
                    principalTable: "Circles",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CircleWishlists",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CircleId = table.Column<int>(type: "integer", nullable: false),
                WishlistId = table.Column<int>(type: "integer", nullable: false),
                PublicId = table.Column<string>(type: "text", nullable: false, defaultValueSql: "gen_random_uuid()"),
                Deleted = table.Column<bool>(type: "boolean", nullable: false),
                RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())"),
                UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc', now())")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CircleWishlists", x => x.Id);
                table.ForeignKey(
                    name: "FK_CircleWishlists_Circles_CircleId",
                    column: x => x.CircleId,
                    principalTable: "Circles",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_CircleWishlists_Wishlists_WishlistId",
                    column: x => x.WishlistId,
                    principalTable: "Wishlists",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CircleMembers_CircleId_UserId",
            table: "CircleMembers",
            columns: new[] { "CircleId", "UserId" });

        migrationBuilder.CreateIndex(
            name: "IX_CircleMembers_UserId",
            table: "CircleMembers",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Circles_OwnerId",
            table: "Circles",
            column: "OwnerId");

        migrationBuilder.CreateIndex(
            name: "IX_Circles_PublicId",
            table: "Circles",
            column: "PublicId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CircleWishlists_CircleId_WishlistId",
            table: "CircleWishlists",
            columns: new[] { "CircleId", "WishlistId" });

        migrationBuilder.CreateIndex(
            name: "IX_CircleWishlists_WishlistId",
            table: "CircleWishlists",
            column: "WishlistId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CircleMembers");

        migrationBuilder.DropTable(
            name: "CircleWishlists");

        migrationBuilder.DropTable(
            name: "Circles");

        migrationBuilder.DropColumn(
            name: "CircleId",
            table: "WishlistPermissions");
    }
}
