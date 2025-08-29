using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookDataApi.Shared.Models;
using System.Text.Json.Serialization;

namespace book_data_api.Models
{
    public class Book : BookDataApi.Shared.Models.Book
    {
        // Navigation property for one-to-many relationship with BookReview
        [JsonIgnore]
        public ICollection<BookReview> BookReviews { get; set; } = new List<BookReview>();
        
        // Navigation property for many-to-many relationship with Bookshelf
        [JsonIgnore]
        public ICollection<Bookshelf> Bookshelves { get; set; } = new List<Bookshelf>();
        
        // Navigation property for many-to-many relationship with Tone
        [JsonIgnore]
        public ICollection<Tone> Tones { get; set; } = new List<Tone>();
        
        // Navigation property for one-to-many relationship with BookToneRecommendation
        [JsonIgnore]
        public ICollection<BookToneRecommendation> BookToneRecommendations { get; set; } = new List<BookToneRecommendation>();
                
        [JsonIgnore]
        public BookCoverImage? CoverImage { get; set; }  // Navigation property for the associated image

        [NotMapped]
        public new string TitleByAuthor => $"{Title} by {AuthorFirstName} {AuthorLastName}".Trim();
    }
} 