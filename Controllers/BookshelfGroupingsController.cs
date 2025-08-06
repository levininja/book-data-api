using book_data_api.Data;
using book_data_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookshelfGroupingsController : ControllerBase
    {
        private readonly ILogger<BookshelfGroupingsController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookshelfGroupingsController(ILogger<BookshelfGroupingsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBookshelfGroupings()
        {
            try
            {
                var groupings = await _context.BookshelfGroupings
                    .Include(bg => bg.Bookshelves)
                    .OrderBy(bg => bg.DisplayName ?? bg.Name)
                    .ToListAsync();
                
                return Ok(groupings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookshelf groupings");
                return StatusCode(500, "An error occurred while fetching bookshelf groupings");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookshelfGrouping(int id)
        {
            try
            {
                var grouping = await _context.BookshelfGroupings
                    .Include(bg => bg.Bookshelves)
                    .FirstOrDefaultAsync(bg => bg.Id == id);
                
                if (grouping == null)
                    return NotFound();
                
                return Ok(grouping);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookshelf grouping with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the bookshelf grouping");
            }
        }
    }
} 