using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookToneRecommendationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BookToneRecommendations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ToneId",
                table: "BookToneRecommendations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookToneRecommendation_ToneId",
                table: "BookToneRecommendations",
                column: "ToneId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookToneRecommendations_Tones_ToneId",
                table: "BookToneRecommendations",
                column: "ToneId",
                principalTable: "Tones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookToneRecommendations_Tones_ToneId",
                table: "BookToneRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_BookToneRecommendation_ToneId",
                table: "BookToneRecommendations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BookToneRecommendations");

            migrationBuilder.DropColumn(
                name: "ToneId",
                table: "BookToneRecommendations");
        }
    }
}
