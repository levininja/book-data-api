namespace BookDataApi.Dtos
{
    public class BookCoverImageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DateTime DateDownloaded { get; set; }
    }
} 