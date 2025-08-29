using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookToneRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookToneRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Tone = table.Column<string>(type: "text", nullable: false),
                    Feedback = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookToneRecommendations", x => x.Id);
                    table.CheckConstraint("CK_BookToneRecommendation_Feedback", "\"Feedback\" BETWEEN -2 AND 1");
                    table.ForeignKey(
                        name: "FK_BookToneRecommendations_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookToneRecommendation_BookId_Tone",
                table: "BookToneRecommendations",
                columns: new[] { "BookId", "Tone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookToneRecommendations");
        }
    }
}
