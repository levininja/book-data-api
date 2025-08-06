using book_data_api.Models;

namespace book_data_api.Services
{
    public interface IBookReviewSearchService
    {
        Task<List<BookReview>> SearchBooksAsync(string searchTerm);
    }
} 