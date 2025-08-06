using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using book_data_api.Data;
using book_data_api.Models;
using BookDataApi.Dtos;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToneConfigurationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ToneConfigurationController> _logger;

        public ToneConfigurationController(ApplicationDbContext context, ILogger<ToneConfigurationController> logger)
        {
            _context = context;
            _logger = logger;
        }


        /// <summary>
        /// Retrieves the tone configuration data from the database and returns it as a list of ToneItemDto objects.
        /// Loads parent tones with their subtones and maps them to DTOs for the configuration API.
        /// </summary>
        /// <returns>List of ToneItemDto objects representing the tone hierarchy</returns>
        [HttpGet]
        public async Task<ActionResult<List<ToneItemDto>>> GetToneConfiguration()
        {
            List<Tone> tones = await _context.Tones
                .Include(t => t.Subtones)
                .Where(t => t.ParentId == null) // This way you don't get subtones twice in the structure
                .OrderBy(t => t.Name)
                .ToListAsync();
                
            List<ToneItemDto> toneDtos = tones.Select(t => new ToneItemDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Subtones = t.Subtones.Select(st => new ToneItemDto
                {
                    Id = st.Id,
                    Name = st.Name,
                    Description = st.Description,
                    ParentId = st.ParentId
                }).OrderBy(st => st.Name).ToList()
            }).OrderBy(t => t.Name).ToList();
            
            return Ok(toneDtos);
        }

        /// <summary>
        /// Handles POST requests for tone configuration updates, including creating, updating, and removing tones and subtones.
        /// </summary>
        /// <param name="tones">The list of ToneItemDto objects containing tone configuration data</param>
        /// <returns>Boolean indicating success or failure of the operation</returns>
        [HttpPost]
        public async Task<ActionResult<bool>> UpdateToneConfiguration([FromBody] List<ToneItemDto> tones)
        {
            try
            {
                // Get all existing tones
                List<Tone> existingTones = await _context.Tones
                    .Include(t => t.Subtones)
                    .ToListAsync();
                
                // Handle tone removals...
                List<Tone> tonesToRemove = existingTones
                    .Where(et => tones.Any(mt => mt.Id == et.Id && mt.ShouldRemove))
                    .ToList();
                
                // Also remove subtones of removed parent tones
                List<Tone> subtonesToRemove = existingTones
                    .Where(et => et.ParentId.HasValue && tonesToRemove.Any(tr => tr.Id == et.ParentId))
                    .ToList();
                
                _context.Tones.RemoveRange(tonesToRemove);
                _context.Tones.RemoveRange(subtonesToRemove);
                
                // Process each tone in the model that isn't marked for removal
                foreach (ToneItemDto toneModel in tones.Where(t => !t.ShouldRemove))
                {
                    Tone tone;
                    
                    if (toneModel.Id > 0)
                    {
                        // Update existing tone
                        tone = existingTones.First(et => et.Id == toneModel.Id);
                        tone.Name = toneModel.Name;
                        tone.Description = toneModel.Description;
                    }
                    else
                    {
                        // Create new tone
                        tone = new Tone
                        {
                            Name = toneModel.Name,
                            Description = toneModel.Description
                        };
                        _context.Tones.Add(tone);
                    }
                    
                    // Handle subtones...
                    List<Tone> existingSubtones = existingTones.Where(et => et.ParentId == tone.Id).ToList();
                    
                    // Remove subtones marked for removal
                    List<Tone> subtonesToRemoveForThisTone = existingSubtones
                        .Where(es => toneModel.Subtones.Any(st => st.Id == es.Id && st.ShouldRemove))
                        .ToList();
                    _context.Tones.RemoveRange(subtonesToRemoveForThisTone);
                    
                    // Process remaining subtones
                    foreach (ToneItemDto subtoneModel in toneModel.Subtones.Where(st => !st.ShouldRemove))
                    {
                        if (subtoneModel.Id > 0)
                        {
                            // Update existing subtone
                            Tone subtone = existingSubtones.First(es => es.Id == subtoneModel.Id);
                            subtone.Name = subtoneModel.Name;
                            subtone.Description = subtoneModel.Description;
                        }
                        else
                        {
                            // Create new subtone
                            Tone subtone = new Tone
                            {
                                Name = subtoneModel.Name,
                                Description = subtoneModel.Description,
                                Parent = tone
                            };
                            _context.Tones.Add(subtone);
                        }
                    }
                }
                
                // Save all changes to the database in one fell swoop
                await _context.SaveChangesAsync();
                
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving tone configuration");
                return BadRequest(false);
            }
        }
    }
}