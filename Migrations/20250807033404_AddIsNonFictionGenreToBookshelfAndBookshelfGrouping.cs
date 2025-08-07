using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsNonFictionGenreToBookshelfAndBookshelfGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNonFictionGenre",
                table: "Bookshelves",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNonFictionGenre",
                table: "BookshelfGroupings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNonFictionGenre",
                table: "Bookshelves");

            migrationBuilder.DropColumn(
                name: "IsNonFictionGenre",
                table: "BookshelfGroupings");
        }
    }
}
