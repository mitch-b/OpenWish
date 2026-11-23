using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenWish.Data.Migrations;

/// <inheritdoc />
public partial class _20261123124551_AddWishlistArchive : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsArchived",
            table: "Wishlists",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsArchived",
            table: "Wishlists");
    }
}
