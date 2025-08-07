namespace book_data_api.Models
{
    public class Bookshelf
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public bool? Display { get; set; }
        public bool IsGenreBased { get; set; } = false;
        
        // Navigation property for many-to-many relationship with Book
        public ICollection<Book> Books { get; set; } = new List<Book>();
        
        // Navigation property for many-to-many relationship with BookshelfGrouping
        public ICollection<BookshelfGrouping> BookshelfGroupings { get; set; } = new List<BookshelfGrouping>();
    }
} 