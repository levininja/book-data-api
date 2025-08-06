using book_data_api.Data;
using book_data_api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace book_data_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TonesController : ControllerBase
    {
        private readonly ILogger<TonesController> _logger;
        private readonly ApplicationDbContext _context;
        
        public TonesController(ILogger<TonesController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetTones()
        {
            try
            {
                var tones = await _context.Tones
                    .Include(t => t.Parent)
                    .Include(t => t.Subtones)
                    .Include(t => t.Books)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
                
                return Ok(tones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tones");
                return StatusCode(500, "An error occurred while fetching tones");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTone(int id)
        {
            try
            {
                var tone = await _context.Tones
                    .Include(t => t.Parent)
                    .Include(t => t.Subtones)
                    .Include(t => t.Books)
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (tone == null)
                    return NotFound();
                
                return Ok(tone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tone with ID: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the tone");
            }
        }
    }
} 