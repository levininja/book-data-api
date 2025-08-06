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
    public class ToneConfigurationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TonesController> _logger;
        // For the uncategorized tones that aren't part of any other group
        private readonly string otherTonesGroupName = "Other";

        public ToneConfigurationController(ApplicationDbContext context, ILogger<ToneConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Gets the tone configuration including all tones with their hierarchical structure.
        /// </summary>
        /// <returns>List of tone items with their subtones</returns>
        [HttpGet("configuration")]
        public async Task<ActionResult<List<ToneItemDto>>> GetToneConfiguration()
        {
            try
            {
                // Get all tones with their relationships
                List<Tone> allTones = await _context.Tones
                    .Include(t => t.Subtones)
                    .Where(t => t.ParentId == null) // Get only parent tones
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                // Convert to DTOs
                List<ToneItemDto> toneDtos = allTones.Select(tone => new ToneItemDto
                {
                    Id = tone.Id,
                    Name = tone.Name,
                    Description = tone.Description,
                    ParentId = tone.ParentId,
                    ShouldRemove = false,
                    Subtones = tone.Subtones.OrderBy(st => st.Name).Select(subtone => new ToneItemDto
                    {
                        Id = subtone.Id,
                        Name = subtone.Name,
                        Description = subtone.Description,
                        ParentId = subtone.ParentId,
                        ShouldRemove = false,
                        Subtones = new List<ToneItemDto>() // Subtones don't have their own subtones
                    }).ToList()
                }).ToList();

                return Ok(toneDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tone configuration");
                return StatusCode(500, "An error occurred while retrieving the tone configuration.");
            }
        }

        /// <summary>
        /// Updates the tone configuration based on the provided tone list.
        /// </summary>
        /// <param name="tones">List of tone items to update</param>
        /// <returns>True if the update was successful, false otherwise</returns>
        [HttpPost("configuration")]
        public async Task<ActionResult<bool>> UpdateToneConfiguration([FromBody] List<ToneItemDto> tones)
        {
            try
            {
                if (tones == null)
                {
                    return BadRequest("Tone configuration data is required.");
                }

                // Get all existing tones
                List<Tone> existingTones = await _context.Tones.ToListAsync();

                // Process each tone in the request
                foreach (ToneItemDto toneDto in tones)
                {
                    if (toneDto.ShouldRemove)
                    {
                        // Remove tone if marked for removal
                        Tone? toneToRemove = existingTones.FirstOrDefault(t => t.Id == toneDto.Id);
                        if (toneToRemove != null)
                        {
                            _context.Tones.Remove(toneToRemove);
                        }
                    }
                    else
                    {
                        // Update or create tone
                        Tone? existingTone = existingTones.FirstOrDefault(t => t.Id == toneDto.Id);
                        if (existingTone != null)
                        {
                            // Update existing tone
                            existingTone.Name = toneDto.Name;
                            existingTone.Description = toneDto.Description;
                            existingTone.ParentId = toneDto.ParentId;
                        }
                        else
                        {
                            // Create new tone
                            Tone newTone = new Tone
                            {
                                Name = toneDto.Name,
                                Description = toneDto.Description,
                                ParentId = toneDto.ParentId
                            };
                            _context.Tones.Add(newTone);
                        }

                        // Process subtones
                        if (toneDto.Subtones != null)
                        {
                            foreach (ToneItemDto subtoneDto in toneDto.Subtones)
                            {
                                if (subtoneDto.ShouldRemove)
                                {
                                    // Remove subtone if marked for removal
                                    Tone? subtoneToRemove = existingTones.FirstOrDefault(t => t.Id == subtoneDto.Id);
                                    if (subtoneToRemove != null)
                                    {
                                        _context.Tones.Remove(subtoneToRemove);
                                    }
                                }
                                else
                                {
                                    // Update or create subtone
                                    Tone? existingSubtone = existingTones.FirstOrDefault(t => t.Id == subtoneDto.Id);
                                    if (existingSubtone != null)
                                    {
                                        // Update existing subtone
                                        existingSubtone.Name = subtoneDto.Name;
                                        existingSubtone.Description = subtoneDto.Description;
                                        existingSubtone.ParentId = toneDto.Id; // Set parent to current tone
                                    }
                                    else
                                    {
                                        // Create new subtone
                                        Tone newSubtone = new Tone
                                        {
                                            Name = subtoneDto.Name,
                                            Description = subtoneDto.Description,
                                            ParentId = toneDto.Id // Set parent to current tone
                                        };
                                        _context.Tones.Add(newSubtone);
                                    }
                                }
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tone configuration");
                return StatusCode(500, "An error occurred while updating the tone configuration.");
            }
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
        /// Analyzes a book review to suggest appropriate tones based on bookshelf tags, review content, and genre associations.
        /// </summary>
        /// <param name="bookReview">The book review to analyze</param>
        /// <param name="allTones">All available tones for matching</param>
        /// <param name="genreToneAssociations">Genre-tone associations for genre-based suggestions</param>
        /// <returns>List of suggested tone IDs</returns>
        private List<int> GetSuggestedTones(BookReview bookReview, List<Tone> allTones, List<GenreToneAssociation> genreToneAssociations)
        {
            HashSet<int> suggestions = new HashSet<int>();
            
            // Get bookshelf names for matching
            List<string> bookshelfNames = bookReview.Bookshelves.Select(bs => bs.Name.ToLower()).ToList();
            string searchableContent = (bookReview.SearchableString ?? "").ToLower();
            string reviewContent = (bookReview.MyReview ?? "").ToLower();
            
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
                    GenreToneAssociation? association = genreToneAssociations.FirstOrDefault(gta => 
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
        /// Gets the genres for a book review based on its bookshelf groupings
        /// </summary>
        /// <param name="bookReview">The book review to get genres for</param>
        /// <returns>List of genre names</returns>
        private List<string> GetBookGenres(BookReview bookReview)
        {
            return bookReview.Bookshelves
                .SelectMany(bs => bs.BookshelfGroupings)
                .Select(bg => bg.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
        }

        /// <summary>
        /// Returns a list of genre-tone associations for suggesting tones based on book genres.
        /// </summary>
        /// <returns>List of genre-tone associations</returns>
        private List<GenreToneAssociation> GetGenreToneAssociations()
        {
            return new List<GenreToneAssociation>
            {
                new GenreToneAssociation
                {
                    Genre = "Fantasy",
                    Tones = new List<string> { "Epic", "Heroic", "Mystical", "Atmospheric", "Poignant", "Dark", "Bittersweet", "Whimsical", "Dramatic", "Haunting" }
                },
                new GenreToneAssociation
                {
                    Genre = "Science Fiction",
                    Tones = new List<string> { "Philosophical", "Epic", "Psychological", "Intense", "Suspenseful", "Dark", "Realistic", "Surreal", "Bittersweet", "Uplifting" }
                },
                new GenreToneAssociation
                {
                    Genre = "Historical Fiction",
                    Tones = new List<string> { "Poignant", "Melancholic", "Bittersweet", "Heartwarming", "Tragic", "Realistic", "Dramatic", "Atmospheric", "Haunting", "Romantic", "Gritty", "Detached" }
                },
                new GenreToneAssociation
                {
                    Genre = "Thriller",
                    Tones = new List<string> { "Intense", "Suspenseful", "Psychological", "Dark", "Gritty", "Claustrophobic", "Dramatic", "Cynical", "Disturbing", "Detached" }
                },
                new GenreToneAssociation
                {
                    Genre = "Horror",
                    Tones = new List<string> { "Horrific", "Disturbing", "Macabre", "Grotesque", "Claustrophobic", "Haunting", "Dark", "Psychological", "Surreal", "Unsettling", "Tragic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Contemporary",
                    Tones = new List<string> { "Realistic", "Detached", "Poignant", "Heartwarming", "Bittersweet", "Romantic", "Angsty", "Sweet", "Playful", "Uplifting" }
                },
                new GenreToneAssociation
                {
                    Genre = "Literary",
                    Tones = new List<string> { "Poignant", "Melancholic", "Bittersweet", "Psychological", "Detached", "Philosophical", "Realistic", "Dark", "Tragic", "Haunting" }
                },
                new GenreToneAssociation
                {
                    Genre = "Romance",
                    Tones = new List<string> { "Romantic", "Steamy", "Sweet", "Angsty", "Flirty", "Poignant", "Heartwarming", "Uplifting", "Playful", "Bittersweet", "Whimsical", "Dramatic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Mystery",
                    Tones = new List<string> { "Suspenseful", "Psychological", "Dark", "Intense", "Realistic", "Gritty", "Detached", "Hard-boiled", "Atmospheric", "Dramatic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Magical Realism",
                    Tones = new List<string> { "Surreal", "Mystical", "Lyrical", "Poignant", "Haunting", "Bittersweet", "Whimsical", "Atmospheric", "Philosophical", "Playful", "Detached" }
                },
                new GenreToneAssociation
                {
                    Genre = "Fairy Tale",
                    Tones = new List<string> { "Whimsical", "Mystical", "Atmospheric", "Romantic", "Heroic", "Poignant", "Heartwarming", "Playful", "Dark", "Tragic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Humor",
                    Tones = new List<string> { "Playful", "Flirty", "Sweet", "Whimsical", "Upbeat", "Cynical", "Detached", "Dramatic", "Realistic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Children's",
                    Tones = new List<string> { "Whimsical", "Playful", "Cozy", "Heartwarming", "Sweet", "Hopeful", "Uplifting", "Mystical", "Dramatic", "Poignant", "Atmospheric" }
                },
                new GenreToneAssociation
                {
                    Genre = "Dystopian",
                    Tones = new List<string> { "Dark", "Bleak", "Gritty", "Cynical", "Disturbing", "Grimdark", "Intense", "Suspenseful", "Tragic", "Poignant", "Haunting", "Philosophical", "Epic" }
                },
                new GenreToneAssociation
                {
                    Genre = "Paranormal",
                    Tones = new List<string> { "Mystical", "Haunting", "Dark", "Romantic", "Tragic", "Suspenseful", "Atmospheric", "Psychological", "Grotesque", "Poignant" }
                },
                new GenreToneAssociation
                {
                    Genre = "Crime",
                    Tones = new List<string> { "Gritty", "Hard-boiled", "Suspenseful", "Intense", "Dark", "Detached", "Realistic", "Cynical", "Psychological", "Dramatic", "Disturbing" }
                },
                new GenreToneAssociation
                {
                    Genre = "Classics",
                    Tones = new List<string> { "Philosophical", "Poignant", "Tragic", "Melancholic", "Detached", "Psychological", "Realistic", "Epic", "Bittersweet", "Romantic", "Cynical", "Haunting" }
                }
            };
        }