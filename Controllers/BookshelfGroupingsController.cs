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
        
        
        /// <summary>
        /// Displays the bookshelf configuration page with current bookshelves and groupings.
        /// </summary>
        /// <returns>The bookshelf configuration view with populated view model.</returns>
        [HttpGet]
        public async Task<IActionResult> BookshelfConfiguration()
        {
            List<Bookshelf> bookshelves = await _context.Bookshelves
                .OrderBy(bs => bs.DisplayName ?? bs.Name)
                .ToListAsync();
                
            List<BookshelfGrouping> groupings = await _context.BookshelfGroupings
                .Include(bg => bg.Bookshelves)
                .OrderBy(bg => bg.DisplayName ?? bg.Name)
                .ToListAsync();
                
            BookshelfConfigurationViewModel viewModel = new BookshelfConfigurationViewModel
            {
                EnableCustomMappings = bookshelves.Any(bs => bs.Display.HasValue),
                Bookshelves = bookshelves.Select(bs => new BookshelfDisplayItem
                {
                    Id = bs.Id,
                    Name = bs.Name,
                    DisplayName = bs.DisplayName,
                    Display = bs.Display ?? false,
                    IsGenreBased = bs.IsGenreBased
                }).ToList(),
                Groupings = groupings.Select(bg => new BookshelfGroupingItem
                {
                    Id = bg.Id,
                    Name = bg.Name,
                    DisplayName = bg.DisplayName,
                    SelectedBookshelfIds = bg.Bookshelves.Select(bs => bs.Id).ToList(),
                    IsGenreBased = bg.IsGenreBased
                }).ToList()
            };
            
            return View(viewModel);
        }
        
        /// <summary>
        /// Handles the POST request for updating bookshelf configurations, including display settings and groupings.
        /// </summary>
        /// <param name="model">The view model containing updated bookshelf configuration data.</param>
        /// <returns>Redirect to the bookshelf configuration page after saving.</returns>
        [HttpPost]
        public async Task<IActionResult> BookshelfConfiguration(BookshelfConfigurationViewModel model)
        {
            try
            {                
                // Update bookshelf display settings
                List<Bookshelf> bookshelves = await _context.Bookshelves.ToListAsync();
                
                if (model.EnableCustomMappings)
                {
                    foreach (Bookshelf bookshelf in bookshelves)
                    {
                        BookshelfDisplayItem? displayItem = model.Bookshelves.FirstOrDefault(b => b.Id == bookshelf.Id);
                        bookshelf.Display = displayItem?.Display ?? false;
                        bookshelf.IsGenreBased = displayItem?.IsGenreBased ?? false;
                    }
                }
                else // Reset all display settings to null when custom mappings are disabled
                    foreach (Bookshelf bookshelf in bookshelves)
                        bookshelf.Display = null;
                
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
                foreach (BookshelfGroupingItem groupingModel in model.Groupings)
                {                   
                    BookshelfGrouping grouping;
                    
                    if (groupingModel.Id > 0)
                    {
                        grouping = existingGroupings.First(eg => eg.Id == groupingModel.Id);
                        grouping.Name = groupingModel.Name;
                        grouping.DisplayName = groupingModel.DisplayName;
                        grouping.IsGenreBased = groupingModel.IsGenreBased;
                        grouping.Bookshelves.Clear();
                    }
                    else
                    {
                        grouping = new BookshelfGrouping
                        {
                            Name = groupingModel.Name,
                            DisplayName = groupingModel.DisplayName,
                            IsGenreBased = groupingModel.IsGenreBased
                        };
                        _context.BookshelfGroupings.Add(grouping);
                    }
                    
                    // Add selected bookshelves to the grouping
                    List<Bookshelf> selectedBookshelves = bookshelves
                        .Where(bs => groupingModel.SelectedBookshelfIds?.Contains(bs.Id) ?? false)
                        .ToList();
                        
                    foreach (Bookshelf bookshelf in selectedBookshelves)
                    {
                        grouping.Bookshelves.Add(bookshelf);
                        bookshelvesInGroupings.Add(bookshelf.Id);
                        // Set IsGenreBased for bookshelves in groupings
                        bookshelf.IsGenreBased = groupingModel.IsGenreBased;
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
                ViewBag.SuccessMessage = "Bookshelf configuration saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bookshelf configuration");
                ModelState.AddModelError("", "An error occurred while saving the configuration.");
            }
            
            return await BookshelfConfiguration();
        }

    }
} 