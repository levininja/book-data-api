using book_data_api.Data;
using book_data_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookDataApi.Shared.Dtos;
using BookDataApi.Shared.Models;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/bookshelfgroupings")]
    public class BookshelfGroupingsController : ControllerBase
    {
        private readonly ILogger<BookshelfGroupingsController> _logger;
        private readonly ApplicationDbContext _context;
        
        public BookshelfGroupingsController(ILogger<BookshelfGroupingsController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        
        /// <summary>
        /// Retrieves the bookshelf configuration data from the database and returns it as a BookshelfConfigurationDto object.
        /// </summary>
        /// <returns>BookshelfConfigurationDto object containing bookshelves and groupings configuration</returns>
        [HttpGet]
        public async Task<ActionResult<BookshelfConfigurationDto>> GetBookshelfConfiguration()
        {
            List<Bookshelf> bookshelves = await _context.Bookshelves
                .OrderBy(bs => bs.Name)
                .ToListAsync();
                
            List<BookshelfGrouping> groupings = await _context.BookshelfGroupings
                .Include(bg => bg.Bookshelves)
                .OrderBy(bg => bg.Name)
                .ToListAsync();
                
            BookshelfConfigurationDto configurationDto = new BookshelfConfigurationDto
            {
                EnableCustomMappings = true,
                Bookshelves = bookshelves.Select(bs => new BookshelfDisplayItemDto
                {
                    Id = bs.Id,
                    Name = bs.Name,
                    Display = bs.Display,
                    IsGenreBased = bs.IsGenreBased,
                    IsNonFictionGenre = bs.IsNonFictionGenre
                }).ToList(),
                Groupings = groupings.Select(bg => new BookshelfGroupingItemDto
                {
                    Id = bg.Id,
                    Name = bg.Name,
                    BookshelfIds = bg.Bookshelves.Select(bs => bs.Id).ToList(),
                    IsGenreBased = bg.IsGenreBased,
                    IsNonFictionGenre = bg.IsNonFictionGenre
                }).ToList()
            };
            
            return Ok(configurationDto);
        }
        
        /// <summary>
        /// Handles the POST request for updating bookshelf configurations, including display settings and groupings.
        /// </summary>
        /// <param name="model">The BookshelfConfigurationDto containing updated bookshelf configuration data.</param>
        /// <returns>Boolean indicating success or failure of the operation</returns>
        [HttpPost]
        public async Task<ActionResult<bool>> UpdateBookshelfConfiguration([FromBody] BookshelfConfigurationDto model)
        {
            try
            {                
                // Update bookshelf display settings
                List<Bookshelf> bookshelves = await _context.Bookshelves.ToListAsync();
                
                if (model.EnableCustomMappings)
                    foreach (Bookshelf bookshelf in bookshelves)
                    {
                        BookshelfDisplayItemDto? displayItem = model.Bookshelves.FirstOrDefault(b => b.Id == bookshelf.Id);
                        bookshelf.Display = displayItem?.Display ?? false;
                        bookshelf.IsGenreBased = displayItem?.IsGenreBased ?? false;
                        bookshelf.IsNonFictionGenre = displayItem?.IsNonFictionGenre ?? false;
                    }
                
                // Handle groupings
                List<BookshelfGrouping> existingGroupings = await _context.BookshelfGroupings
                    .Include(bg => bg.Bookshelves)
                    .ToListAsync();
                
                // Only remove groupings that are explicitly marked for removal
                List<BookshelfGrouping> groupingsToRemove = existingGroupings
                    .Where(eg => model.Groupings.Any(mg => mg.Id == eg.Id && mg.ShouldRemove))
                    .ToList();
                    
                _context.BookshelfGroupings.RemoveRange(groupingsToRemove);
                
                // Track which bookshelves are assigned to groupings for automatic display setting
                HashSet<int> bookshelvesInGroupings = new HashSet<int>();
                
                // Update or create groupings
                foreach (BookshelfGroupingItemDto groupingModel in model.Groupings)
                {                   
                    BookshelfGrouping grouping;
                    
                    if (groupingModel.Id > 0)
                    {
                        grouping = existingGroupings.First(eg => eg.Id == groupingModel.Id);
                        grouping.Name = groupingModel.Name;
                        grouping.IsGenreBased = groupingModel.IsGenreBased;
                        grouping.IsNonFictionGenre = groupingModel.IsNonFictionGenre;
                        grouping.Bookshelves.Clear();
                    }
                    else
                    {
                        grouping = new BookshelfGrouping
                        {
                            Name = groupingModel.Name,
                            IsGenreBased = groupingModel.IsGenreBased,
                            IsNonFictionGenre = groupingModel.IsNonFictionGenre
                        };
                        _context.BookshelfGroupings.Add(grouping);
                    }
                    
                    // Add selected bookshelves to the grouping
                    List<Bookshelf> selectedBookshelves = bookshelves
                        .Where(bs => groupingModel.BookshelfIds?.Contains(bs.Id) ?? false)
                        .ToList();
                        
                    foreach (Bookshelf bookshelf in selectedBookshelves)
                    {
                        grouping.Bookshelves.Add(bookshelf);
                        bookshelvesInGroupings.Add(bookshelf.Id);
                        // Set IsGenreBased and IsNonFictionGenre for bookshelves in groupings
                        bookshelf.IsGenreBased = groupingModel.IsGenreBased;
                        bookshelf.IsNonFictionGenre = groupingModel.IsNonFictionGenre;
                    }
                }
                
                // Automatically set bookshelves to display if they're assigned to a grouping
                if (model.EnableCustomMappings)
                {
                    foreach (Bookshelf bookshelf in bookshelves)
                    {
                        if (bookshelvesInGroupings.Contains(bookshelf.Id))
                        {
                            bookshelf.Display = true;
                        }
                        // If IsGenreBased is true, ensure Display is also true
                        if (bookshelf.IsGenreBased)
                        {
                            bookshelf.Display = true;
                        }
                    }
                }
                
                int saveResult = await _context.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bookshelf configuration");
                return BadRequest(false);
            }
        }

    }
} 