namespace BookDataApi.Dtos
{
    public class BookshelfConfigurationDto
    {
        public bool EnableCustomMappings { get; set; }
        public List<BookshelfDisplayItemDto> Bookshelves { get; set; } = new List<BookshelfDisplayItemDto>();
        public List<BookshelfGroupingItemDto> Groupings { get; set; } = new List<BookshelfGroupingItemDto>();
    }
}