using System.Text.Json.Serialization;

namespace book_data_api.Models
{
    public class Bookshelf
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool Display { get; set; } = true;
        public bool IsGenreBased { get; set; } = false;
        public bool IsNonFictionGenre { get; set; } = false;
        
        // Navigation property for many-to-many relationship with Book
        [JsonIgnore]
        public ICollection<Models.Book> Books { get; set; } = new List<Models.Book>();
        
        // Navigation property for many-to-many relationship with BookshelfGrouping
        [JsonIgnore]
        public ICollection<BookshelfGrouping> BookshelfGroupings { get; set; } = new List<BookshelfGrouping>();
    }
} 