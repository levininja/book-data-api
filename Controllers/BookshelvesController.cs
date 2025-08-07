using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/bookshelves")]
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
                    .OrderBy(bs => bs.Name)
                    .ToListAsync();
                
                var groupings = await _context.BookshelfGroupings
                    .Include(bg => bg.Bookshelves)
                    .OrderBy(bg => bg.Name)
                    .ToListAsync();
                
                var bookshelfDtos = bookshelves.Select(bs => new BookshelfDisplayItemDto
                {
                    Id = bs.Id,
                    Name = bs.Name,
                    Display = bs.Display ?? true,
                    IsGenreBased = bs.IsGenreBased
                }).ToList();
                
                var groupingDtos = groupings.Select(bg => new BookshelfGroupingItemDto
                {
                    Id = bg.Id,
                    Name = bg.Name,
                    SelectedBookshelfIds = bg.Bookshelves.Select(bs => bs.Id).ToList(),
                    ShouldRemove = false,
                    IsGenreBased = bg.IsGenreBased
                }).ToList();
                
                var configuration = new BookshelfConfigurationDto
                {
                    EnableCustomMappings = true, // You may want to make this configurable
                    Bookshelves = bookshelfDtos,
                    Groupings = groupingDtos
                };
                
                return Ok(configuration);
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