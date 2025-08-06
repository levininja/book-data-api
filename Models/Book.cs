using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace book_data_api.Models
{
    public class Book
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string AuthorFirstName { get; set; }
        public required string AuthorLastName { get; set; }
        public string? ISBN10 { get; set; }
        public string? ISBN13 { get; set; }
        public required decimal AverageRating { get; set; } // 1-5
        public int? NumberOfPages { get; set; }
        public int? OriginalPublicationYear { get; set; }
        public string? SearchableString { get; set; }
        
        // Navigation property for one-to-many relationship with BookReview
        public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
        
        // Navigation property for many-to-many relationship with Bookshelf
        public ICollection<Bookshelf> Bookshelves { get; set; } = new List<Bookshelf>();
        
        // Navigation property for many-to-many relationship with Tone
        public ICollection<Tone> Tones { get; set; } = new List<Tone>();
                
        public BookCoverImage? CoverImage { get; set; }  // Navigation property for the associated image
        public int? CoverImageId { get; set; }  // Foreign key (nullable to allow books without images)

        [NotMapped]
        public string TitleByAuthor => $"{Title} by {AuthorFirstName} {AuthorLastName}".Trim();
    }
} 