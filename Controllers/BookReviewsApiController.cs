using book_data_api.Data;
using book_data_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/bookreviews")]
    public class BookReviewsApiController : ControllerBase
    {
        private readonly ILogger<BookReviewsApiController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookReviewsApiController(
            ILogger<BookReviewsApiController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBookReviews(string? displayCategory, string? shelf, string? grouping, bool recent = false)
        {
            try
            {
                // Check if custom mappings are enabled
                bool useCustomMappings = await _context.Bookshelves.AnyAsync(bs => bs.Display.HasValue);
                
                // Get bookshelves and groupings based on custom mapping settings
                List<Bookshelf> allBookshelves;
                List<BookshelfGrouping> allBookshelfGroupings = new List<BookshelfGrouping>();
                
                if (useCustomMappings)
                {
                    // Only show bookshelves that are marked for display and not in any grouping
                    var bookshelvesInGroupings = await _context.BookshelfGroupings
                        .SelectMany(bg => bg.Bookshelves.Select(bs => bs.Id))
                        .ToListAsync();
                        
                    allBookshelves = await _context.Bookshelves
                        .Where(bs => bs.Display == true && !bookshelvesInGroupings.Contains(bs.Id))
                        .OrderBy(bs => bs.DisplayName ?? bs.Name)
                        .ToListAsync();
                        
                    allBookshelfGroupings = await _context.BookshelfGroupings
                        .Include(bg => bg.Bookshelves)
                        .OrderBy(bg => bg.DisplayName ?? bg.Name)
                        .ToListAsync();
                }
                else
                {
                    // Show all bookshelves as before
                    allBookshelves = await _context.Bookshelves
                        .OrderBy(bs => bs.DisplayName ?? bs.Name)
                        .ToListAsync();
                }
                
                // Default to "favorites" shelf if no shelf/grouping is specified and not showing recent
                if (string.IsNullOrEmpty(shelf) && string.IsNullOrEmpty(grouping) && !recent)
                    shelf = "favorites";
                
                // Build the query for book reviews - only include reviews with content
                var bookReviewsQuery = _context.BookReviews
                    .Include(br => br.Book)
                    .Include(br => br.Book.Bookshelves)
                    .Include(br => br.Book.Tones)
                    .Include(br => br.Book.CoverImage)
                    .Where(br => br.HasReviewContent == true)
                    .AsQueryable();
                
                // Apply filters
                if (recent)
                {
                    bookReviewsQuery = bookReviewsQuery
                        .OrderByDescending(r => r.DateRead)
                        .Take(10);
                }
                else if (!string.IsNullOrEmpty(grouping))
                {
                    // Filter by grouping - get all bookshelves in the grouping
                    var groupingBookshelfNames = await _context.BookshelfGroupings
                        .Where(bg => bg.Name.ToLower() == grouping.ToLower())
                        .SelectMany(bg => bg.Bookshelves.Select(bs => bs.Name))
                        .ToListAsync();
                        
                    bookReviewsQuery = bookReviewsQuery
                        .Where(br => br.Book.Bookshelves.Any(bs => groupingBookshelfNames.Contains(bs.Name)));
                }
                else if (!string.IsNullOrEmpty(shelf))
                {
                    // Filter by individual shelf
                    bookReviewsQuery = bookReviewsQuery
                        .Where(br => br.Book.Bookshelves.Any(bs => bs.Name.ToLower() == shelf.ToLower()));
                }
                
                if (!recent)
                {
                    bookReviewsQuery = bookReviewsQuery.OrderByDescending(r => r.DateRead);
                }
                
                var bookReviews = await bookReviewsQuery.ToListAsync();
                
                var result = new
                {
                    Category = displayCategory,
                    AllBookshelves = allBookshelves.Select(bs => new { bs.Id, bs.Name, bs.IsGenreBased }).ToList(),
                    AllBookshelfGroupings = allBookshelfGroupings.Select(bg => new 
                    { 
                        bg.Id, 
                        bg.Name,
                        bg.IsGenreBased,
                        Bookshelves = bg.Bookshelves.Select(bs => new { bs.Id, bs.Name, bs.IsGenreBased }).ToList()
                    }).ToList(),
                    SelectedShelf = shelf,
                    SelectedGrouping = grouping,
                    ShowRecentOnly = recent,
                    UseCustomMappings = useCustomMappings,
                    BookReviews = bookReviews.Select(br => new
                    {
                        Id = br.Id,
                        Title = br.Book.Title,
                        AuthorFirstName = br.Book.AuthorFirstName,
                        AuthorLastName = br.Book.AuthorLastName,
                        TitleByAuthor = br.Book.TitleByAuthor,
                        ReviewerRating = br.ReviewerRating,
                        AverageRating = br.Book.AverageRating,
                        NumberOfPages = br.Book.NumberOfPages,
                        OriginalPublicationYear = br.Book.OriginalPublicationYear,
                        DateRead = br.DateRead,
                        Review = br.Review,
                        SearchableString = br.Book.SearchableString,
                        HasReviewContent = br.HasReviewContent,
                        PreviewText = br.ReviewPreviewText,
                        ReadingTimeMinutes = br.ReadingTimeMinutes,
                        CoverImageId = br.Book.CoverImageId,
                        Bookshelves = br.Book.Bookshelves.Select(bs => new { bs.Id, bs.Name, bs.IsGenreBased }).ToList(),
                        Tones = br.Book.Tones.Select(t => new { t.Id, t.Name, t.Description }).ToList()
                    }).ToList()
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book reviews");
                return StatusCode(500, "An error occurred while fetching book reviews");
            }
        }
        

    }
} 