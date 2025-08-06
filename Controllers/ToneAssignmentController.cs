using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Dtos;
using BookDataApi.Dtos.ToneAssignmentDtos;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                List<BookReview> allBookReviews = await _context.BookReviews
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Tones)
                    .Include(br => br.Book)
                        .ThenInclude(b => b.Bookshelves)
                            .ThenInclude(bs => bs.BookshelfGroupings)
                    .Where(br => br.HasReviewContent)
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

                // Put the tone groupings together
                List<ToneGroupDto> toneGroups = allTones.Where(pt => pt.Subtones.Any()).Select((pt, index) => new ToneGroupDto
                {
                    Name = pt.Name,
                    DisplayName = pt.Name,
                    ColorClass = GetColorClassForTone(index),
                    Tones = new[] { pt }.Concat(pt.Subtones.OrderBy(st => st.Name))
                        .Select(t => new ToneDisplayItemDto
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Description = t.Description
                        }).ToList()
                }).ToList();

                // Add a group for all stand-alone tones that don't have children
                toneGroups.Add(new ToneGroupDto
                {
                    Name = otherTonesGroupName,
                    DisplayName = otherTonesGroupName,
                    ColorClass = GetColorClassForTone(-1), // -1 is a special case for the uncategorized tones group
                    Tones = allTones.Where(pt => !pt.Subtones.Any() && pt.ParentId == null).Select(pt => new ToneDisplayItemDto {
                        Id = pt.Id,
                        Name = pt.Name,
                        Description = pt.Description
                    }).ToList()
                });

                List<GenreToneAssociationDto> genreToneAssociations = GetGenreToneAssociations();

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
                        AssignedToneIds = br.Book.Tones.Select(t => t.Id).ToList(),
                        SuggestedToneIds = GetSuggestedTones(br, allTones, genreToneAssociations)
                    }).ToList(),
                    BooksWithTones = booksWithTones.Select(br => new BookReviewToneItemDto
                    {
                        Id = br.Id,
                        Title = br.Book.Title,
                        AuthorName = $"{br.Book.AuthorFirstName} {br.Book.AuthorLastName}".Trim(),
                        Genres = GetBookGenres(br),
                        MyReview = br.Review,
                        AssignedToneIds = br.Book.Tones.Select(t => t.Id).ToList(),
                        SuggestedToneIds = GetSuggestedTones(br, allTones, genreToneAssociations)
                    }).ToList(),
                    ToneGroups = toneGroups
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
                            bookReview.Book.Tones.Add(tone);
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
                            bookReview.Book.Tones.Add(tone);
                    }
                }

                await _context.SaveChangesAsync();
                
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

        /// <summary>
        /// Analyzes a book review to suggest appropriate tones based on bookshelf tags, review content, and genre associations.
        /// </summary>
        /// <param name="bookReview">The book review to analyze</param>
        /// <param name="allTones">All available tones for matching</param>
        /// <param name="genreToneAssociations">Genre-tone associations for genre-based suggestions</param>
        /// <returns>List of suggested tone IDs</returns>
        private List<int> GetSuggestedTones(BookReview bookReview, List<Tone> allTones, List<GenreToneAssociationDto> genreToneAssociations)
        {
            HashSet<int> suggestions = new HashSet<int>();
            
            // Get bookshelf names for matching
            List<string> bookshelfNames = bookReview.Book.Bookshelves.Select(bs => bs.Name.ToLower()).ToList();
            string searchableContent = (bookReview.Book.SearchableString ?? "").ToLower();
            string reviewContent = (bookReview.Review ?? "").ToLower();
            
            // Get genres for this book
            List<string> genres = GetBookGenres(bookReview);
            
            foreach (Tone tone in allTones.Concat(allTones.SelectMany(t => t.Subtones)))
            {
                string toneName = tone.Name.ToLower();
                
                // Check if tone name matches bookshelf names
                if (bookshelfNames.Any(bn => bn.Contains(toneName)))
                {
                    suggestions.Add(tone.Id);
                    continue;
                }
                
                // Check if tone name appears in searchable content
                if (searchableContent.Contains(toneName))
                {
                    suggestions.Add(tone.Id);
                    continue;
                }
                
                // Check if tone name appears in review content
                if (reviewContent.Contains(toneName))
                {
                    suggestions.Add(tone.Id);
                    continue;
                }
                
                // Check genre-based suggestions
                foreach (string genre in genres)
                {
                    GenreToneAssociationDto? association = genreToneAssociations.FirstOrDefault(gta => 
                        gta.Genre.Equals(genre, StringComparison.OrdinalIgnoreCase));
                    if (association != null && association.Tones.Any(t => 
                        t.Equals(tone.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        suggestions.Add(tone.Id);
                        break;
                    }
                }
            }
            
            return suggestions.ToList();
        }

        /// <summary>
        /// Returns a CSS class name for color coding tone groups based on the tone's index.
        /// </summary>
        /// <param name="toneIndex">The index of the tone in the list of parent tones. Used to consistently assign colors. Special case of -1 for 
        /// tones that don't have a parent tone (i.e. are not subtones).</param>
        /// <returns>CSS class name for the tone group color. Returns "tone-grey" for tones that don't have a parent tone (i.e. are not subtones), 
        /// otherwise returns one of 8 predefined color classes.</returns>
        private string GetColorClassForTone(int toneIndex)
        {
            // Special case for tones that don't have a parent tone (i.e. are not subtones)
            if (toneIndex == -1)
                return "tone-grey";

            // Generate consistent pastel colors based on tone index
            string[] colors = new[] { "tone-blue", "tone-purple",  "tone-aqua",  "tone-teal", "tone-orange", "tone-pink", "tone-yellow", "tone-green", "tone-red" };
            return colors[toneIndex];
        }

        /// <summary>
        /// Returns a list of genre-tone associations for suggesting tones based on book genres.
        /// </summary>
        /// <returns>List of genre-tone associations</returns>
        private List<GenreToneAssociationDto> GetGenreToneAssociations()
        {
            return new List<GenreToneAssociationDto>
            {
                new GenreToneAssociationDto
                {
                    Genre = "Fantasy",
                    Tones = new List<string> { "Epic", "Heroic", "Mystical", "Atmospheric", "Poignant", "Dark", "Bittersweet", "Whimsical", "Dramatic", "Haunting" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Science Fiction",
                    Tones = new List<string> { "Philosophical", "Epic", "Psychological", "Intense", "Suspenseful", "Dark", "Realistic", "Surreal", "Bittersweet", "Uplifting" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Historical Fiction",
                    Tones = new List<string> { "Poignant", "Melancholic", "Bittersweet", "Heartwarming", "Tragic", "Realistic", "Dramatic", "Atmospheric", "Haunting", "Romantic", "Gritty", "Detached" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Thriller",
                    Tones = new List<string> { "Intense", "Suspenseful", "Psychological", "Dark", "Gritty", "Claustrophobic", "Dramatic", "Cynical", "Disturbing", "Detached" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Horror",
                    Tones = new List<string> { "Horrific", "Disturbing", "Macabre", "Grotesque", "Claustrophobic", "Haunting", "Dark", "Psychological", "Surreal", "Unsettling", "Tragic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Contemporary",
                    Tones = new List<string> { "Realistic", "Detached", "Poignant", "Heartwarming", "Bittersweet", "Romantic", "Angsty", "Sweet", "Playful", "Uplifting" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Literary",
                    Tones = new List<string> { "Poignant", "Melancholic", "Bittersweet", "Psychological", "Detached", "Philosophical", "Realistic", "Dark", "Tragic", "Haunting" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Romance",
                    Tones = new List<string> { "Romantic", "Steamy", "Sweet", "Angsty", "Flirty", "Poignant", "Heartwarming", "Uplifting", "Playful", "Bittersweet", "Whimsical", "Dramatic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Mystery",
                    Tones = new List<string> { "Suspenseful", "Psychological", "Dark", "Intense", "Realistic", "Gritty", "Detached", "Hard-boiled", "Atmospheric", "Dramatic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Magical Realism",
                    Tones = new List<string> { "Surreal", "Mystical", "Lyrical", "Poignant", "Haunting", "Bittersweet", "Whimsical", "Atmospheric", "Philosophical", "Playful", "Detached" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Fairy Tale",
                    Tones = new List<string> { "Whimsical", "Mystical", "Atmospheric", "Romantic", "Heroic", "Poignant", "Heartwarming", "Playful", "Dark", "Tragic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Humor",
                    Tones = new List<string> { "Playful", "Flirty", "Sweet", "Whimsical", "Upbeat", "Cynical", "Detached", "Dramatic", "Realistic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Children's",
                    Tones = new List<string> { "Whimsical", "Playful", "Cozy", "Heartwarming", "Sweet", "Hopeful", "Uplifting", "Mystical", "Dramatic", "Poignant", "Atmospheric" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Dystopian",
                    Tones = new List<string> { "Dark", "Bleak", "Gritty", "Cynical", "Disturbing", "Grimdark", "Intense", "Suspenseful", "Tragic", "Poignant", "Haunting", "Philosophical", "Epic" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Paranormal",
                    Tones = new List<string> { "Mystical", "Haunting", "Dark", "Romantic", "Tragic", "Suspenseful", "Atmospheric", "Psychological", "Grotesque", "Poignant" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Crime",
                    Tones = new List<string> { "Gritty", "Hard-boiled", "Suspenseful", "Intense", "Dark", "Detached", "Realistic", "Cynical", "Psychological", "Dramatic", "Disturbing" }
                },
                new GenreToneAssociationDto
                {
                    Genre = "Classics",
                    Tones = new List<string> { "Philosophical", "Poignant", "Tragic", "Melancholic", "Detached", "Psychological", "Realistic", "Epic", "Bittersweet", "Romantic", "Cynical", "Haunting" }
                }
            };
        }
    }
}