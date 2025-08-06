using book_data_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/bookcovers")]
    public class BookCoverController : ControllerBase
    {
        private readonly ILogger<BookCoverController> _logger;
        private readonly IBookCoverService _bookCoverService;
        
        public BookCoverController(
            ILogger<BookCoverController> logger,
            IBookCoverService bookCoverService)
        {
            _logger = logger;
            _bookCoverService = bookCoverService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBookCover(string bookTitle, int bookReviewId)
        {
            if (string.IsNullOrEmpty(bookTitle))
            {
                _logger.LogWarning("Missing search term in book cover request");
                return BadRequest("Search term is required");
            }

            try
            {
                byte[]? imageData = await _bookCoverService.GetBookCoverImageAsync(bookTitle, bookReviewId);
                if (imageData == null)
                    return NotFound();

                return File(imageData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book cover for search term: {bookTitle}", bookTitle);
                return StatusCode(500, "An error occurred while fetching the book cover");
            }
        }
    }
} 