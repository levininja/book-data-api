using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/books")]
    public class BooksController : ControllerBase
    {
        private readonly ILogger<BooksController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BooksController(ILogger<BooksController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            try
            {
                var book = await _context.Books
                    .Include(b => b.Bookshelves)
                    .Include(b => b.Tones)
                    .Include(b => b.CoverImage)
                    .FirstOrDefaultAsync(b => b.Id == id);
                
                if (book == null)
                    return NotFound();
                
                var bookDto = new BookDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    AuthorFirstName = book.AuthorFirstName,
                    AuthorLastName = book.AuthorLastName,
                    ISBN10 = book.ISBN10,
                    ISBN13 = book.ISBN13,
                    AverageRating = book.AverageRating,
                    NumberOfPages = book.NumberOfPages,
                    OriginalPublicationYear = book.OriginalPublicationYear,
                    SearchableString = book.SearchableString,
                    TitleByAuthor = book.TitleByAuthor,
                    CoverImageId = book.CoverImageId,
                    Bookshelves = book.Bookshelves.Select(bs => new BookshelfDto
                    {
                        Id = bs.Id,
                        Name = bs.Name,
                        Display = bs.Display,
                        IsGenreBased = bs.IsGenreBased
                    }).ToList(),
                    Tones = book.Tones.Select(t => new ToneDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description
                    }).ToList(),
                    CoverImage = book.CoverImage != null ? new BookCoverImageDto
                    {
                        Id = book.CoverImage.Id,
                        Name = book.CoverImage.Name,
                        Width = book.CoverImage.Width,
                        Height = book.CoverImage.Height,
                        FileType = book.CoverImage.FileType,
                        DateDownloaded = book.CoverImage.DateDownloaded
                    } : null
                };
                
                return Ok(bookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting book with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the book");
            }
        }
    }
} 