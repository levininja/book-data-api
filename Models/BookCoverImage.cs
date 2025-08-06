using System;

namespace book_data_api.Models
{
    public class BookCoverImage
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DateTime DateDownloaded { get; set; } = DateTime.UtcNow;
    }
} 