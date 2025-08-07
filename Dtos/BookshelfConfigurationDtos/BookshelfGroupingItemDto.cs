namespace BookDataApi.Dtos
{
    public class BookshelfGroupingItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> BookshelfIds { get; set; } = new List<int>();
        public bool ShouldRemove { get; set; }
        public bool IsGenreBased { get; set; }
    }
}