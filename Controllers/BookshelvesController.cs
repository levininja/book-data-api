using book_data_api.Data;
using book_data_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookshelvesController : ControllerBase
    {
        private readonly ILogger<BookshelvesController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookshelvesController(ILogger<BookshelvesController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetBookshelves()
        {
            try
            {
                var bookshelves = await _context.Bookshelves
                    .OrderBy(bs => bs.DisplayName ?? bs.Name)
                    .ToListAsync();
                
                return Ok(bookshelves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookshelves");
                return StatusCode(500, "An error occurred while fetching bookshelves");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookshelf(int id)
        {
            try
            {
                var bookshelf = await _context.Bookshelves
                    .Include(bs => bs.Books)
                    .Include(bs => bs.BookshelfGroupings)
                    .FirstOrDefaultAsync(bs => bs.Id == id);
                
                if (bookshelf == null)
                    return NotFound();
                
                return Ok(bookshelf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bookshelf with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the bookshelf");
            }
        }
    }
} 