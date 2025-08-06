using book_data_api.Data;
using book_data_api.Models;
using Microsoft.EntityFrameworkCore;

namespace book_data_api.Services
{
    public class BookReviewSearchService : IBookReviewSearchService
    {
        private readonly ApplicationDbContext _context;
        
        public BookReviewSearchService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<BookReview>> SearchBooksAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<BookReview>();
            
            var normalizedSearchTerm = searchTerm.ToLower();
            
            return await _context.BookReviews
                .Include(br => br.Book)
                .Include(br => br.Book.Bookshelves)
                .Where(br => br.Book.SearchableString != null && br.Book.SearchableString.Contains(normalizedSearchTerm))
                .OrderByDescending(br => br.DateRead)
                .Take(50) // Limit results for performance
                .ToListAsync();
        }
    }
} 