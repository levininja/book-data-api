using book_data_api.Data;
using book_data_api.Models;
using book_data_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookReviewsController : ControllerBase
    {
        private readonly ILogger<BookReviewsController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookReviewsController(
            ILogger<BookReviewsController> logger,
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
                    // Filter by specific shelf
                    bookReviewsQuery = bookReviewsQuery
                        .Where(br => br.Book.Bookshelves.Any(bs => bs.Name.ToLower() == shelf.ToLower()));
                }
                
                // Apply display category filter if specified
                if (!string.IsNullOrEmpty(displayCategory))
                {
                    bookReviewsQuery = bookReviewsQuery
                        .Where(br => br.Book.Bookshelves.Any(bs => 
                            (bs.DisplayName ?? bs.Name).ToLower() == displayCategory.ToLower()));
                }
                
                // Order by date read (most recent first)
                bookReviewsQuery = bookReviewsQuery.OrderByDescending(r => r.DateRead);
                
                var bookReviews = await bookReviewsQuery.ToListAsync();
                
                return Ok(new
                {
                    bookReviews,
                    bookshelves = allBookshelves,
                    bookshelfGroupings = allBookshelfGroupings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book reviews");
                return StatusCode(500, "An error occurred while fetching book reviews");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookReview(int id)
        {
            try
            {
                var bookReview = await _context.BookReviews
                    .Include(br => br.Book)
                    .Include(br => br.Book.Bookshelves)
                    .Include(br => br.Book.Tones)
                    .Include(br => br.Book.CoverImage)
                    .FirstOrDefaultAsync(br => br.Id == id);
                
                if (bookReview == null)
                    return NotFound();
                
                return Ok(bookReview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book review with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the book review");
            }
        }
        
        [HttpPost("import")]
        public async Task<IActionResult> ImportBookReviews(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    Quote = '"',
                    Mode = CsvMode.RFC4180
                });
                
                List<GoodreadsBookReviewCsv> records;
                try
                {
                    records = csv.GetRecords<GoodreadsBookReviewCsv>().ToList();
                }
                catch (Exception csvEx)
                {
                    _logger.LogError(csvEx, "Error reading CSV records: {Message}", csvEx.Message);
                    return StatusCode(500, $"Error reading CSV: {csvEx.Message}");
                }

                var importedCount = 0;



                foreach (var record in records)
                {
                    try
                    {
                        // Check if book already exists
                        var existingBook = await _context.Books
                            .FirstOrDefaultAsync(b => b.Title == record.Title && 
                                                    b.AuthorFirstName == record.AuthorFirstName && 
                                                    b.AuthorLastName == record.AuthorLastName);

                        Book book;
                        if (existingBook != null)
                        {
                            book = existingBook;
                        }
                        else
                        {
                            // Create new book
                            book = new Book
                            {
                                Title = record.Title ?? "",
                                AuthorFirstName = record.AuthorFirstName ?? "",
                                AuthorLastName = record.AuthorLastName ?? "",
                                ISBN10 = CleanIsbnValue(record.ISBN10),
                                ISBN13 = CleanIsbnValue(record.ISBN13Value),
                                AverageRating = record.AverageRating,
                                NumberOfPages = record.NumberOfPages,
                                OriginalPublicationYear = record.OriginalPublicationYear,
                                SearchableString = $"{record.Title} {record.AuthorFirstName} {record.AuthorLastName}".ToLower()
                            };
                            _context.Books.Add(book);
                            await _context.SaveChangesAsync(); // Save to get the book ID
                        }

                        // Check if review already exists for this book
                        var existingReview = await _context.BookReviews
                            .FirstOrDefaultAsync(br => br.BookId == book.Id && 
                                                     br.DateRead == record.DateRead);

                        if (existingReview == null)
                        {
                            // Create new review - only if there's review content or a rating
                            if (!string.IsNullOrWhiteSpace(record.MyReview) || record.MyRating > 0)
                            {
                                var bookReview = new BookReview
                                {
                                    BookId = book.Id,
                                    ReviewerRating = record.MyRating,
                                    DateRead = record.DateRead,
                                    Review = record.MyReview,
                                    ReviewerFullName = "Levi Hobbs" // Auto-set as requested
                                };
                                _context.BookReviews.Add(bookReview);
                                importedCount++;
                            }
                            else
                            {
                                // Skipping book without review content
                            }
                        }
                        else
                        {
                            // Skipping duplicate review
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing record: {Title} by {Author}", record.Title, $"{record.AuthorFirstName} {record.AuthorLastName}");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully imported {importedCount} book reviews" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing book reviews: {Message}", ex.Message);
                return StatusCode(500, "An error occurred while importing book reviews");
            }
        }

        /// <summary>
        /// Cleans ISBN values by removing common formatting issues like quotes, equals signs, and extra whitespace
        /// </summary>
        /// <param name="isbn">The raw ISBN value from CSV</param>
        /// <returns>Cleaned ISBN value or null if empty</returns>
        private static string? CleanIsbnValue(string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return null;

            // Remove equals signs, quotes, and extra whitespace
            string cleaned = isbn.Trim()
                .Replace("=", "")
                .Replace("\"", "")
                .Replace("'", "");

            // Return null if the result is empty after cleaning
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }
    }
} 