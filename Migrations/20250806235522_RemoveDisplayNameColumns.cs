using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDisplayNameColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Bookshelves");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "BookshelfGroupings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Bookshelves",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "BookshelfGroupings",
                type: "text",
                nullable: true);
        }
    }
}
