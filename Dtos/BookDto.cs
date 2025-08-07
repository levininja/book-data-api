namespace BookDataApi.Dtos
{
    public class BookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorFirstName { get; set; } = string.Empty;
        public string AuthorLastName { get; set; } = string.Empty;
        public string? ISBN10 { get; set; }
        public string? ISBN13 { get; set; }
        public decimal AverageRating { get; set; }
        public int? NumberOfPages { get; set; }
        public int? OriginalPublicationYear { get; set; }
        public string? SearchableString { get; set; }
        public string TitleByAuthor { get; set; } = string.Empty;
        public int? CoverImageId { get; set; }
        public List<BookshelfDto> Bookshelves { get; set; } = new List<BookshelfDto>();
        public List<ToneDto> Tones { get; set; } = new List<ToneDto>();
        public BookCoverImageDto? CoverImage { get; set; }
    }
} 