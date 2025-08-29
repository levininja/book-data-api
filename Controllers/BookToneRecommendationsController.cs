using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Shared.Dtos;
using BookDataApi.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/book-tone-recommendations")]
    public class BookToneRecommendationsController : ControllerBase
    {
        private readonly ILogger<BookToneRecommendationsController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookToneRecommendationsController(ILogger<BookToneRecommendationsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBookToneRecommendations([FromQuery] int? bookId = null, [FromQuery] string? tone = null)
        {
            try
            {
                var query = _context.BookToneRecommendations.AsQueryable();
                
                if (bookId.HasValue)
                {
                    query = query.Where(btr => btr.BookId == bookId.Value);
                }
                
                if (!string.IsNullOrEmpty(tone))
                {
                    query = query.Where(btr => btr.Tone.Contains(tone));
                }
                
                var recommendations = await query
                    .Include(btr => btr.Book)
                    .Select(btr => new BookDataApi.Shared.Dtos.BookToneRecommendationDto
                    {
                        Id = btr.Id,
                        BookId = btr.BookId,
                        Tone = btr.Tone,
                        ToneId = btr.ToneId,
                        Feedback = btr.Feedback,
                        CreatedAt = btr.CreatedAt
                    })
                    .ToListAsync();
                
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book tone recommendations");
                return StatusCode(500, "An error occurred while fetching book tone recommendations");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookToneRecommendation(int id)
        {
            try
            {
                var recommendation = await _context.BookToneRecommendations
                    .Include(btr => btr.Book)
                    .FirstOrDefaultAsync(btr => btr.Id == id);
                
                if (recommendation == null)
                    return NotFound();
                
                var recommendationDto = new BookDataApi.Shared.Dtos.BookToneRecommendationDto
                {
                    Id = recommendation.Id,
                    BookId = recommendation.BookId,
                    Tone = recommendation.Tone,
                    ToneId = recommendation.ToneId,
                    Feedback = recommendation.Feedback,
                    CreatedAt = recommendation.CreatedAt
                };
                
                return Ok(recommendationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book tone recommendation with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the book tone recommendation");
            }
        }
        
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetBookToneRecommendationsByBook(int bookId)
        {
            try
            {
                var recommendations = await _context.BookToneRecommendations
                    .Where(btr => btr.BookId == bookId)
                    .Select(btr => new BookDataApi.Shared.Dtos.BookToneRecommendationDto
                    {
                        Id = btr.Id,
                        BookId = btr.BookId,
                        Tone = btr.Tone,
                        ToneId = btr.ToneId,
                        Feedback = btr.Feedback,
                        CreatedAt = btr.CreatedAt
                    })
                    .ToListAsync();
                
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book tone recommendations for book ID: {BookId}", bookId);
                return StatusCode(500, "An error occurred while fetching book tone recommendations");
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateBookToneRecommendation([FromBody] CreateBookToneRecommendationDto createDto)
        {
            try
            {
                // Verify the book exists
                var book = await _context.Books.FindAsync(createDto.BookId);
                if (book == null)
                {
                    return BadRequest($"Book with ID {createDto.BookId} does not exist");
                }
                
                // Check if a recommendation already exists for this book and tone
                var existingRecommendation = await _context.BookToneRecommendations
                    .FirstOrDefaultAsync(btr => btr.BookId == createDto.BookId && btr.Tone == createDto.Tone);
                
                if (existingRecommendation != null)
                {
                    return BadRequest($"A recommendation for book {createDto.BookId} with tone '{createDto.Tone}' already exists");
                }
                
                var recommendation = new book_data_api.Models.BookToneRecommendation
                {
                    BookId = createDto.BookId,
                    Tone = createDto.Tone,
                    Feedback = createDto.Feedback,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.BookToneRecommendations.Add(recommendation);
                await _context.SaveChangesAsync();
                
                var recommendationDto = new BookDataApi.Shared.Dtos.BookToneRecommendationDto
                {
                    Id = recommendation.Id,
                    BookId = recommendation.BookId,
                    Tone = recommendation.Tone,
                    ToneId = recommendation.ToneId,
                    Feedback = recommendation.Feedback,
                    CreatedAt = recommendation.CreatedAt
                };
                
                return CreatedAtAction(nameof(GetBookToneRecommendation), new { id = recommendation.Id }, recommendationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book tone recommendation");
                return StatusCode(500, "An error occurred while creating the book tone recommendation");
            }
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBookToneRecommendation(int id, [FromBody] UpdateBookToneRecommendationDto updateDto)
        {
            try
            {
                var recommendation = await _context.BookToneRecommendations.FindAsync(id);
                
                if (recommendation == null)
                    return NotFound();
                
                // Update the feedback and optionally the tone ID
                recommendation.Feedback = updateDto.Feedback;
                if (updateDto.ToneId.HasValue)
                {
                    recommendation.ToneId = updateDto.ToneId.Value;
                }
                
                await _context.SaveChangesAsync();
                
                var recommendationDto = new BookDataApi.Shared.Dtos.BookToneRecommendationDto
                {
                    Id = recommendation.Id,
                    BookId = recommendation.BookId,
                    Tone = recommendation.Tone,
                    ToneId = recommendation.ToneId,
                    Feedback = recommendation.Feedback,
                    CreatedAt = recommendation.CreatedAt
                };
                
                return Ok(recommendationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book tone recommendation with ID: {Id}", id);
                return StatusCode(500, "An error occurred while updating the book tone recommendation");
            }
        }
    }
} 