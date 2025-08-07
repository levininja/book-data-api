using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Dtos;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/tones")]
    public class ToneAssignmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ToneAssignmentController> _logger;

        public ToneAssignmentController(ApplicationDbContext context, ILogger<ToneAssignmentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // For the uncategorized tones that aren't part of any other group
        const string otherTonesGroupName = "Other";
                
        /// <summary>
        /// Gets the tone assignment data for book reviews that have review content.
        /// Groups tones by parent tone for organized display and provides suggestions based on content analysis.
        /// </summary>
        /// <returns>The tone assignment data with book reviews and tone groupings</returns>
        [HttpGet("assignment")]
        public async Task<ActionResult<ToneAssignmentDto>> GetToneAssignment()
        {
            try
            {                
                // Get all book reviews that have review content, including their associated books
                // Exclude non-fiction books; these don't have tones assigned to them
                List<BookReview> allBookReviews = await _context.BookReviews
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Tones)
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Bookshelves)
                            .ThenInclude(bs => bs.BookshelfGroupings)
                    .Where(br => br.HasReviewContent)
                    .Where(br => !br.Book.Bookshelves.Any(bs => bs.IsGenreBased && bs.IsNonFictionGenre))
                    .OrderBy(br => br.Book.Title)
                    .ToListAsync();

                // Separate books with and without assigned tones
                List<BookReview> booksWithoutTones = allBookReviews.Where(br => !br.Book.Tones.Any()).ToList();
                List<BookReview> booksWithTones = allBookReviews.Where(br => br.Book.Tones.Any()).ToList();

                // Get all tones with their relationships
                List<Tone> allTones = await _context.Tones
                    .Include(t => t.Subtones)
                    .Where(t => t.ParentId == null) // This way you don't get subtones twice in the structure
                    .ToListAsync();

                // Return the DTO
                ToneAssignmentDto toneAssignmentDto = new ToneAssignmentDto
                {
                    BookReviews = booksWithoutTones.Select(br => new BookReviewToneItemDto
                    {
                        Id = br.Id,
                        Title = br.Book.Title,
                        AuthorName = $"{br.Book.AuthorFirstName} {br.Book.AuthorLastName}".Trim(),
                        Genres = GetBookGenres(br),
                        MyReview = br.Review,
                        AssignedToneIds = br.Book.Tones.Select(t => t.Id).ToList()
                        //SuggestedToneIds = TODO
                    }).ToList(),
                    BooksWithTones = booksWithTones.Select(br => new BookReviewToneItemDto
                    {
                        Id = br.Id,
                        Title = br.Book.Title,
                        AuthorName = $"{br.Book.AuthorFirstName} {br.Book.AuthorLastName}".Trim(),
                        Genres = GetBookGenres(br),
                        MyReview = br.Review,
                        AssignedToneIds = br.Book.Tones.Select(t => t.Id).ToList(),
                        //SuggestedToneIds = TODO
                    }).ToList(),
                    Tones = allTones.Select(t => new ToneItemDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Subtones = t.Subtones.Select(st => new ToneItemDto
                        {
                            Id = st.Id,
                            Name = st.Name
                        }).ToList()
                    }).ToList()
                };
                
                return Ok(toneAssignmentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tone assignment data");
                return StatusCode(500, "An error occurred while retrieving the tone assignment data.");
            }
        }

        /// <summary>
        /// Handles POST requests for tone assignment updates, saving the tone assignments for book reviews.
        /// </summary>
        /// <param name="model">The DTO containing tone assignment data from the form submission</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        [HttpPost("assignment")]
        public async Task<ActionResult<bool>> UpdateToneAssignment([FromBody] ToneAssignmentDto model)
        {
            try
            {
                // Get all book review IDs - include both BookReviews and BooksWithTones
                List<int> allBookReviewIds = model.BookReviews.Select(brm => brm.Id)
                    .Concat(model.BooksWithTones.Select(brm => brm.Id))
                    .ToList();
                
                List<BookReview> bookReviews = await _context.BookReviews
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Tones)
                    .Where(br => allBookReviewIds.Contains(br.Id))
                    .ToListAsync();

                List<Tone> allTones = await _context.Tones.ToListAsync();

                // Update tone assignments for books without tones
                foreach (BookReviewToneItemDto bookReviewModel in model.BookReviews)
                {
                    BookReview? bookReview = bookReviews.FirstOrDefault(br => br.Id == bookReviewModel.Id);
                    
                    if (bookReview != null)
                    {
                        // Clear existing tone assignments
                        bookReview.Book.Tones.Clear();
                        
                        // Add new tone assignments
                        List<Tone> selectedTones = allTones.Where(t => bookReviewModel.AssignedToneIds.Contains(t.Id)).ToList();
                        foreach (Tone tone in selectedTones)
                        {
                            bookReview.Book.Tones.Add(tone);
                        }
                    }
                }

                // Update tone assignments for books with tones (accordion section)
                foreach (BookReviewToneItemDto bookReviewModel in model.BooksWithTones)
                {
                    BookReview? bookReview = bookReviews.FirstOrDefault(br => br.Id == bookReviewModel.Id);
                    
                    if (bookReview != null)
                    {
                        // Clear existing tone assignments
                        bookReview.Book.Tones.Clear();
                        
                        // Add new tone assignments
                        List<Tone> selectedTones = allTones.Where(t => bookReviewModel.AssignedToneIds.Contains(t.Id)).ToList();
                        foreach (Tone tone in selectedTones)
                        {
                            bookReview.Book.Tones.Add(tone);
                        }
                    }
                }

                int saveResult = await _context.SaveChangesAsync();
                
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tone assignments");
                return StatusCode(500, false);
            }
        }

        /// <summary>
        /// Gets the genres for a book review based on its bookshelf groupings
        /// </summary>
        /// <param name="bookReview">The book review to get genres for</param>
        /// <returns>List of genre names</returns>
        private List<string> GetBookGenres(BookReview bookReview)
        {
            return bookReview.Book.Bookshelves
                .SelectMany(bs => bs.BookshelfGroupings)
                .Select(bg => bg.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }
    }
}