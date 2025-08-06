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
    [Route("api/bookreviews")]
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
                int importResult = await ImportBookReviewsFromCsvFile(file);
                return Ok(new { message = $"Successfully imported {importResult} book reviews." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing book reviews");
                return StatusCode(500, ex.Message);
            }
        }
        
        /// <summary>
        /// Process the CSV file and import the book reviews into the database
        /// </summary>
        /// <param name="file">The CSV file to import</param>
        /// <returns>The number of imported book reviews</returns>
        private async Task<int> ImportBookReviewsFromCsvFile(IFormFile file)
        {
            using Stream stream = file.OpenReadStream();
            using StreamReader reader = new StreamReader(stream);
            
            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                Mode = CsvMode.RFC4180,
                PrepareHeaderForMatch = args => args.Header.Trim('"', ' ')
            };
            
            string[] requiredColumns = new[] 
            { 
                "Title", 
                "Author l-f", 
                "My Rating", 
                "Average Rating", 
                "Number of Pages", 
                "Original Publication Year", 
                "Date Read", 
                "Bookshelves", 
                "Exclusive Shelf", 
                "My Review" 
            };
            
            var existingBooks = await _context.Books
                .Select(b => new { b.Title, b.AuthorFirstName, b.AuthorLastName, b.Bookshelves.Count })
                .ToListAsync();
            
            List<Bookshelf> existingBookshelves = await _context.Bookshelves.ToListAsync();
            
            int importedCount = 0;
            int duplicateCount = 0;
            
            using CsvReader csv = new CsvReader(reader, config);
            List<GoodreadsBookReviewCsv> records = csv.GetRecords<GoodreadsBookReviewCsv>().ToList();
            
            for (int i = 0; i < records.Count; i++)
            {
                GoodreadsBookReviewCsv row = records[i];
                
                // Check header on first iteration; this can't be done before the loop begins
                // because of how stream readers work.
                if (i == 0)
                    ValidateCsvHeader(csv, requiredColumns);
                    
                // Skip import if there is no review content
                if (row.My_Review == null || row.My_Review.Trim().Length == 0)
                    continue;
                
                // Parse author name (last, first)
                string authorName = row.Author_l_f ?? "";
                string firstName = "";
                string lastName = "";
                if (authorName.Contains(","))
                {
                    string[] parts = authorName.Split(new[] { ", " }, StringSplitOptions.None);
                    lastName = parts[0].Trim();
                    firstName = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else
                    lastName = authorName;

                // Check for existing books with same title and author
                var matchingBook = existingBooks.FirstOrDefault(b => 
                    b.Title == row.Title && 
                    b.AuthorFirstName == firstName && 
                    b.AuthorLastName == lastName);
                if (matchingBook != null)
                {
                    int importedRowBookshelvesCount = row.Bookshelves?.Split(',', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                    bool hasSameNumberOfBookshelves = matchingBook.Count == importedRowBookshelvesCount;
                    
                    // If it has the same number of bookshelves too, then this is a true duplicate; skip import and increase duplicate count
                    if (hasSameNumberOfBookshelves)
                    {
                        duplicateCount++;
                        continue;
                    }
                    // If it has a different number of bookshelves, we need to delete the existing book so it can be replaced with the new one
                    else
                    {
                        var booksToDelete = await _context.Books
                            .Where(b => b.Title == row.Title && 
                                       b.AuthorFirstName == firstName && 
                                       b.AuthorLastName == lastName)
                            .ToListAsync();
                        _context.Books.RemoveRange(booksToDelete);
                        
                        // Remove from our in-memory tracking list as well
                        existingBooks.RemoveAll(b => 
                            b.Title == row.Title && 
                            b.AuthorFirstName == firstName && 
                            b.AuthorLastName == lastName);
                    }
                }
                
                // Now that we have determined that the book should in fact be imported...

                _logger.LogInformation("Importing book: {Title} by {Author}", row.Title, authorName);
                _logger.LogDebug("Row data: {RowData}", row);
                _logger.LogDebug("Bookshelves: {Bookshelves}", row.Bookshelves?.Split(',', StringSplitOptions.RemoveEmptyEntries).Length ?? 0);

                // Finish parsing other fields
                int.TryParse(row.My_Rating, out int myRating);
                if (myRating < 0 || myRating > 5)
                {
                    _logger.LogWarning("Skipping book '{Title}' - Invalid rating: {Rating}", row.Title, myRating);
                    continue;
                }

                decimal.TryParse(row.Average_Rating, out decimal avgRating);

                int.TryParse(row.Number_of_Pages, out int pages);

                int.TryParse(row.Original_Publication_Year, out int pubYear);

                DateTime dateRead = DateTime.UtcNow;
                if (DateTime.TryParse(row.Date_Read, out DateTime parsedDate))
                    dateRead = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                
                // Create Book object
                Book book = new Book
                {
                    Title = row.Title ?? "",
                    AuthorFirstName = firstName,
                    AuthorLastName = lastName,
                    AverageRating = avgRating,
                    NumberOfPages = pages > 0 ? pages : null,
                    OriginalPublicationYear = pubYear > 0 ? pubYear : null,
                    SearchableString = BuildSearchableString(row.Title ?? "", firstName, lastName, 
                        row.Additional_Authors, row.Publisher, row.Bookshelves)
                };

                // Import book
                _context.Books.Add(book);
                await _context.SaveChangesAsync(); // Save to get the book ID
                
                // Create BookReview object
                BookReview bookReview = new BookReview
                {
                    BookId = book.Id,
                    ReviewerRating = myRating,
                    ReviewerFullName = "Levi Hobbs",
                    DateRead = dateRead,
                    Review = row.My_Review ?? ""
                };

                // Import book review
                _context.BookReviews.Add(bookReview);
                importedCount++;
                
                // Import any bookshelves, if they don't already exist
                ProcessBookshelvesForImport(row.Bookshelves, book, existingBookshelves);
            }
            
            // After processing all records, delete bookshelves that exist in the database but don't exist in the import
            RemoveUnusedBookshelves(records, existingBookshelves);
            
            await _context.SaveChangesAsync();
            return importedCount;
        }

        /// <summary>
        /// Builds a searchable string for a book by combining title, authors, publisher, and processed bookshelves.
        /// </summary>
        /// <param name="title">The book title.</param>
        /// <param name="firstName">The author's first name.</param>
        /// <param name="lastName">The author's last name.</param>
        /// <param name="additionalAuthors">Additional authors.</param>
        /// <param name="publisher">The publisher.</param>
        /// <param name="bookshelves">The bookshelves.</param>
        /// <returns>A lowercase searchable string.</returns>
        private string BuildSearchableString(string title, string firstName, string lastName, 
            string? additionalAuthors, string? publisher, string? bookshelves)
        {
            List<string> searchableParts = new List<string>();
            
            // Add title and author
            searchableParts.Add(title);
            searchableParts.Add($"{firstName} {lastName}".Trim());
            
            // Add additional authors
            if (!string.IsNullOrEmpty(additionalAuthors))
                searchableParts.Add(additionalAuthors);
            
            // Add publisher
            if (!string.IsNullOrEmpty(publisher))
                searchableParts.Add(publisher);
            
            // Process bookshelves
            if (!string.IsNullOrEmpty(bookshelves))
            {
                string processedShelves = ProcessBookshelvesForSearch(bookshelves);
                if (!string.IsNullOrEmpty(processedShelves))
                    searchableParts.Add(processedShelves);
            }
            
            return string.Join(" ", searchableParts.Where(p => !string.IsNullOrEmpty(p))).ToLower();
        }

        /// <summary>
        /// Processes bookshelves for search by filtering, normalizing, and applying synonyms.
        /// </summary>
        /// <param name="bookshelves">The raw bookshelves string.</param>
        /// <returns>A space-separated string of processed shelf names.</returns>
        private string ProcessBookshelvesForSearch(string bookshelves)
        {
            HashSet<string> excludedShelves = new HashSet<string> 
            { 
                "to-read", "to-look-into", "currently-reading", 
                "decided-not-to-read", "anticipating-release" 
            };
            
            Dictionary<string, string> synonymMap = new Dictionary<string, string>
            {
                { "sf", "Science Fiction Sci-Fi Scifi" }
            };
            
            IEnumerable<string> shelves = bookshelves.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower())
                .Where(s => !excludedShelves.Contains(s))
                .Select(s => s.Replace('-', ' '))
                .Select(s => synonymMap.ContainsKey(s) ? synonymMap[s] : s)
                .Where(s => !string.IsNullOrEmpty(s));
            
            return string.Join(" ", shelves);
        }

        /// <summary>
        /// Validates that the CSV header contains all required columns.
        /// </summary>
        /// <param name="csv">The CSV reader.</param>
        /// <param name="requiredColumns">Array of required column names.</param>
        /// <exception cref="Exception">Thrown if header is missing or required columns are absent.</exception>
        private void ValidateCsvHeader(CsvReader csv, string[] requiredColumns)
        {
            string[]? header = csv.HeaderRecord;
            if (header == null)
                throw new Exception("CSV file is missing header row");
            
            List<string> missingColumns = requiredColumns
                .Where(col => !header.Contains(col))
                .ToList();
                
            if (missingColumns.Any())
                throw new Exception($"CSV file is missing required columns: {string.Join(", ", missingColumns)}");
        }

        /// <summary>
        /// Processes bookshelves from the CSV import, creating new bookshelves if they don't exist and associating them with the book.
        /// </summary>
        /// <param name="bookshelvesString">The comma-separated string of bookshelf names from the CSV.</param>
        /// <param name="book">The book to associate the bookshelves with.</param>
        /// <param name="existingBookshelves">The list of existing bookshelves in the database.</param>
        private void ProcessBookshelvesForImport(string? bookshelvesString, Book book, List<Bookshelf> existingBookshelves)
        {
            if (string.IsNullOrEmpty(bookshelvesString))
                return;

            List<string> shelfNames = bookshelvesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            
            foreach (string shelfName in shelfNames)
            {
                string normalizedName = shelfName.ToLower();

                // See if this bookshelf already exists in the database
                Bookshelf? bookshelf = existingBookshelves.FirstOrDefault(b => b.Name.ToLower() == normalizedName);

                // If the bookshelf doesn't exist, create a new one
                if(bookshelf == null){
                    bookshelf = new Bookshelf
                    {
                        Name = shelfName,
                        DisplayName = shelfName
                    };
                    _context.Bookshelves.Add(bookshelf);
                    existingBookshelves.Add(bookshelf);
                }

                // Either way (whether we created a new bookshelf or found an existing one), add it to the book
                // to track that it has this bookshelf (this populates a crossreference table in the db).
                book.Bookshelves.Add(bookshelf);
            }
        }

        /// <summary>
        /// Removes bookshelves that exist in the database but are not present in the import records.
        /// </summary>
        /// <param name="records">The list of imported book review records.</param>
        /// <param name="existingBookshelves">The list of existing bookshelves in the database.</param>
        private void RemoveUnusedBookshelves(List<GoodreadsBookReviewCsv> records, List<Bookshelf> existingBookshelves)
        {
            HashSet<string> importedBookshelfNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (GoodreadsBookReviewCsv record in records)
            {
                if (!string.IsNullOrEmpty(record.Bookshelves))
                {
                    List<string> shelfNames = record.Bookshelves.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    
                    foreach (string shelfName in shelfNames)
                        importedBookshelfNames.Add(shelfName);
                }
            }
            
            // Find bookshelves that exist in the database but not in the import
            List<Bookshelf> bookshelvesToDelete = existingBookshelves
                .Where(bs => !importedBookshelfNames.Contains(bs.Name))
                .ToList();
            
            if (bookshelvesToDelete.Any())
            {
                _context.Bookshelves.RemoveRange(bookshelvesToDelete);
                _logger.LogInformation("Deleted {Count} bookshelves that were not present in the import: {Names}", 
                    bookshelvesToDelete.Count, 
                    string.Join(", ", bookshelvesToDelete.Select(bs => bs.Name)));
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