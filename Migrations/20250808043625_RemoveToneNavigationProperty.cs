using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveToneNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookToneRecommendations_Tones_ToneId",
                table: "BookToneRecommendations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_BookToneRecommendations_Tones_ToneId",
                table: "BookToneRecommendations",
                column: "ToneId",
                principalTable: "Tones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
