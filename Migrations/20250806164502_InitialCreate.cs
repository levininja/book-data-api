using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace book_data_api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookCoverImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ImageData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    DateDownloaded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCoverImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookshelfGroupings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    IsGenreBased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookshelfGroupings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookshelves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Display = table.Column<bool>(type: "boolean", nullable: true),
                    IsGenreBased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookshelves", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LogLevel = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: false),
                    LogDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tones_Tones_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Tones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    AuthorFirstName = table.Column<string>(type: "text", nullable: false),
                    AuthorLastName = table.Column<string>(type: "text", nullable: false),
                    ISBN10 = table.Column<string>(type: "text", nullable: true),
                    ISBN13 = table.Column<string>(type: "text", nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    NumberOfPages = table.Column<int>(type: "integer", nullable: true),
                    OriginalPublicationYear = table.Column<int>(type: "integer", nullable: true),
                    SearchableString = table.Column<string>(type: "text", nullable: true),
                    CoverImageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.CheckConstraint("CK_Book_AverageRating", "\"AverageRating\" BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_Books_BookCoverImages_CoverImageId",
                        column: x => x.CoverImageId,
                        principalTable: "BookCoverImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BookshelfGroupingBookshelves",
                columns: table => new
                {
                    BookshelfGroupingsId = table.Column<int>(type: "integer", nullable: false),
                    BookshelvesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookshelfGroupingBookshelves", x => new { x.BookshelfGroupingsId, x.BookshelvesId });
                    table.ForeignKey(
                        name: "FK_BookshelfGroupingBookshelves_BookshelfGroupings_BookshelfGr~",
                        column: x => x.BookshelfGroupingsId,
                        principalTable: "BookshelfGroupings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookshelfGroupingBookshelves_Bookshelves_BookshelvesId",
                        column: x => x.BookshelvesId,
                        principalTable: "Bookshelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookBookshelves",
                columns: table => new
                {
                    BooksId = table.Column<int>(type: "integer", nullable: false),
                    BookshelvesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookBookshelves", x => new { x.BooksId, x.BookshelvesId });
                    table.ForeignKey(
                        name: "FK_BookBookshelves_Books_BooksId",
                        column: x => x.BooksId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookBookshelves_Bookshelves_BookshelvesId",
                        column: x => x.BookshelvesId,
                        principalTable: "Bookshelves",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReviewerRating = table.Column<int>(type: "integer", nullable: false),
                    ReviewerFullName = table.Column<string>(type: "text", nullable: false),
                    DateRead = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Review = table.Column<string>(type: "text", nullable: true),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    HasReviewContent = table.Column<bool>(type: "boolean", nullable: false, computedColumnSql: "CASE WHEN \"Review\" IS NOT NULL AND \"Review\" != '' THEN true ELSE false END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookReviews", x => x.Id);
                    table.CheckConstraint("CK_BookReview_ReviewerRating", "\"ReviewerRating\" BETWEEN 0 AND 5");
                    table.ForeignKey(
                        name: "FK_BookReviews_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookTones",
                columns: table => new
                {
                    BooksId = table.Column<int>(type: "integer", nullable: false),
                    TonesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookTones", x => new { x.BooksId, x.TonesId });
                    table.ForeignKey(
                        name: "FK_BookTones_Books_BooksId",
                        column: x => x.BooksId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookTones_Tones_TonesId",
                        column: x => x.TonesId,
                        principalTable: "Tones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookBookshelves_BookshelvesId",
                table: "BookBookshelves",
                column: "BookshelvesId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCoverImages_Name",
                table: "BookCoverImages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookReview_HasReviewContent",
                table: "BookReviews",
                column: "HasReviewContent",
                filter: "\"HasReviewContent\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_BookReviews_BookId",
                table: "BookReviews",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Book_SearchableString",
                table: "Books",
                column: "SearchableString");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CoverImageId",
                table: "Books",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_BookshelfGroupingBookshelves_BookshelvesId",
                table: "BookshelfGroupingBookshelves",
                column: "BookshelvesId");

            migrationBuilder.CreateIndex(
                name: "IX_BookshelfGroupings_Name",
                table: "BookshelfGroupings",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookshelves_Name",
                table: "Bookshelves",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookTones_TonesId",
                table: "BookTones",
                column: "TonesId");

            migrationBuilder.CreateIndex(
                name: "IX_Tones_Name",
                table: "Tones",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tones_ParentId",
                table: "Tones",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookBookshelves");

            migrationBuilder.DropTable(
                name: "BookReviews");

            migrationBuilder.DropTable(
                name: "BookshelfGroupingBookshelves");

            migrationBuilder.DropTable(
                name: "BookTones");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "BookshelfGroupings");

            migrationBuilder.DropTable(
                name: "Bookshelves");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Tones");

            migrationBuilder.DropTable(
                name: "BookCoverImages");
        }
    }
}
