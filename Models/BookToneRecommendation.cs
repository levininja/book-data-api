using BookDataApi.Shared.Models;

namespace book_data_api.Models
{
    public class BookToneRecommendation : BookDataApi.Shared.Models.BookToneRecommendation
    {
        // Navigation property for relationship with Book
        public Book Book { get; set; } = null!;
    }
} 