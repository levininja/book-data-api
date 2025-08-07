namespace BookDataApi.Dtos
{
    public class BookshelfDisplayItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Display { get; set; }
        public bool IsGenreBased { get; set; }
    }
}