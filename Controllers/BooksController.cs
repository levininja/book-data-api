using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Shared.Dtos;
using BookDataApi.Shared.Models;
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
        
        [HttpGet]
        public async Task<IActionResult> GetBooks([FromQuery] string? search = null, [FromQuery] int? page = 1, [FromQuery] int? pageSize = 20)
        {
            try
            {
                var query = _context.Books
                    .Include(b => b.Bookshelves)
                    .Include(b => b.Tones)
                    .Include(b => b.CoverImage)
                    .AsQueryable();
                
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => b.SearchableString != null && b.SearchableString.Contains(search));
                }
                
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize!.Value);
                
                var books = await query
                    .Skip((page!.Value - 1) * pageSize!.Value)
                    .Take(pageSize.Value)
                    .Select(book => new BookDto
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
                    })
                    .ToListAsync();
                
                var response = new
                {
                    Books = books,
                    Pagination = new
                    {
                        Page = page!.Value,
                        PageSize = pageSize!.Value,
                        TotalCount = totalCount,
                        TotalPages = totalPages
                    }
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting books");
                return StatusCode(500, "An error occurred while fetching books");
            }
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
        
        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto createDto)
        {
            try
            {
                var book = new book_data_api.Models.Book
                {
                    Title = createDto.Title,
                    AuthorFirstName = createDto.AuthorFirstName,
                    AuthorLastName = createDto.AuthorLastName,
                    ISBN10 = createDto.ISBN10,
                    ISBN13 = createDto.ISBN13,
                    AverageRating = createDto.AverageRating,
                    NumberOfPages = createDto.NumberOfPages,
                    OriginalPublicationYear = createDto.OriginalPublicationYear,
                    SearchableString = createDto.SearchableString,
                    CoverImageId = createDto.CoverImageId
                };
                
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                
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
                    Bookshelves = new List<BookshelfDto>(),
                    Tones = new List<ToneDto>(),
                    CoverImage = null
                };
                
                return CreatedAtAction(nameof(GetBook), new { id = book.Id }, bookDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating book");
                return StatusCode(500, "An error occurred while creating the book");
            }
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto updateDto)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                
                if (book == null)
                    return NotFound();
                
                if (updateDto.Title != null)
                    book.Title = updateDto.Title;
                if (updateDto.AuthorFirstName != null)
                    book.AuthorFirstName = updateDto.AuthorFirstName;
                if (updateDto.AuthorLastName != null)
                    book.AuthorLastName = updateDto.AuthorLastName;
                if (updateDto.ISBN10 != null)
                    book.ISBN10 = updateDto.ISBN10;
                if (updateDto.ISBN13 != null)
                    book.ISBN13 = updateDto.ISBN13;
                if (updateDto.AverageRating.HasValue)
                    book.AverageRating = updateDto.AverageRating.Value;
                if (updateDto.NumberOfPages.HasValue)
                    book.NumberOfPages = updateDto.NumberOfPages;
                if (updateDto.OriginalPublicationYear.HasValue)
                    book.OriginalPublicationYear = updateDto.OriginalPublicationYear;
                if (updateDto.SearchableString != null)
                    book.SearchableString = updateDto.SearchableString;
                if (updateDto.CoverImageId.HasValue)
                    book.CoverImageId = updateDto.CoverImageId;
                
                await _context.SaveChangesAsync();
                
                // Return the updated book with all related data
                return await GetBook(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating book with ID: {Id}", id);
                return StatusCode(500, "An error occurred while updating the book");
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var book = await _context.Books.FindAsync(id);
                
                if (book == null)
                    return NotFound();
                
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting book with ID: {Id}", id);
                return StatusCode(500, "An error occurred while deleting the book");
            }
        }
    }
} 