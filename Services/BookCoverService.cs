using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using book_data_api.Data;
using book_data_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace book_data_api.Services
{
    public class BookCoverService : IBookCoverService
    {
        private readonly ILogger<BookCoverService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;
        private readonly GoogleCustomSearchSettings _settings;

        public BookCoverService(
            ILogger<BookCoverService> logger,
            HttpClient httpClient,
            ApplicationDbContext dbContext,
            IOptions<GoogleCustomSearchSettings> settings)
        {
            _logger = logger;
            _httpClient = httpClient;
            _dbContext = dbContext;
            _settings = settings.Value;
            
            if (string.IsNullOrEmpty(_settings.ApiKey) || string.IsNullOrEmpty(_settings.SearchEngineId))
            {
                throw new InvalidOperationException("Google Custom Search settings are not properly configured");
            }

            // Add a User-Agent header to handle sites that require it (like Wikimedia)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; LeviHobbsBookCoverBot/1.0; +https://levihobbs.com)");
        }

        /// <summary>
        /// Called when we don't yet have a high-quality image for a book stored in the db.
        /// Gets a book cover image from the Google Custom Search API, returns it, and also
        /// stores it in the db for future use.
        /// </summary>
        /// <param name="searchTerm">The title and author of the book to search for</param>
        /// <returns>The image data for the book cover, or null if no image is found</returns>
        public async Task<byte[]?> GetBookCoverImageAsync(string searchTerm, int bookReviewId)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return null;
            
            // Check if we already have this image in the database
            BookCoverImage? existingImage = await _dbContext.BookCoverImages
                .FirstOrDefaultAsync(i => i.Name == searchTerm);
                
            if (existingImage != null)
            {
                // Log warning about duplicate image fetch attempt
                _logger.LogWarning("GetBookCoverImageAsync called for '{SearchTerm}' which already exists in database; shouldn't be happening.", searchTerm);

                await AssociateBookWithCoverImage(bookReviewId, existingImage);

                return existingImage.ImageData;
            }
            
            // If not in database, fetch from Google
            try
            {
                // Build Google Custom Search API URL
                string searchUrl = $"https://www.googleapis.com/customsearch/v1?key={_settings.ApiKey}&cx={_settings.SearchEngineId}&q={Uri.EscapeDataString(searchTerm + " book cover")}&searchType=image&imgType=photo&imgSize=large&num=1";
                
                HttpResponseMessage response = await _httpClient.GetAsync(searchUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Google Custom Search API returned status code: {StatusCode}", response.StatusCode);
                    return null;
                }
                
                string jsonResponse = await response.Content.ReadAsStringAsync();
                GoogleCustomSearchResponse? searchResponse = JsonSerializer.Deserialize<GoogleCustomSearchResponse>(jsonResponse);
                
                if (searchResponse?.Items == null || !searchResponse.Items.Any())
                {
                    _logger.LogWarning("No images found for search term: {SearchTerm}", searchTerm);
                    return null;
                }
                
                GoogleCustomSearchItem firstItem = searchResponse.Items.First();
                string imageUrl = firstItem.Link;
                
                // Download the image
                HttpResponseMessage imageResponse = await _httpClient.GetAsync(imageUrl);
                
                if (!imageResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to download image from URL: {ImageUrl}, Status: {StatusCode}", imageUrl, imageResponse.StatusCode);
                    return null;
                }
                
                byte[] imageData = await imageResponse.Content.ReadAsByteArrayAsync();
                
                // Process the image to ensure it's a reasonable size and format
                using (MemoryStream inputStream = new MemoryStream(imageData))
                using (MemoryStream outputStream = new MemoryStream())
                {
                    using (Image image = await Image.LoadAsync(inputStream))
                    {
                        // Resize if too large (max 800px width/height)
                        if (image.Width > 800 || image.Height > 800)
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(800, 800),
                                Mode = ResizeMode.Max
                            }));
                        }
                        
                        // Save as JPEG for consistency
                        await image.SaveAsJpegAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                        {
                            Quality = 85
                        });
                    }
                    
                    imageData = outputStream.ToArray();
                }
                
                // Store in database
                BookCoverImage newImage = new BookCoverImage
                {
                    Name = searchTerm,
                    ImageData = imageData,
                    Width = 800, // We'll set this to the actual size later if needed
                    Height = 800,
                    FileType = "image/jpeg",
                    DateDownloaded = DateTime.UtcNow
                };
                
                _dbContext.BookCoverImages.Add(newImage);
                await _dbContext.SaveChangesAsync();
                
                // Associate with the book
                await AssociateBookWithCoverImage(bookReviewId, newImage);
                
                return imageData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching book cover for search term: {SearchTerm}", searchTerm);
                return null;
            }
        }
        
        private async Task AssociateBookWithCoverImage(int bookReviewId, BookCoverImage image)
        {
            BookReview? bookReview = await _dbContext.BookReviews
                .Include(br => br.Book)
                .FirstOrDefaultAsync(br => br.Id == bookReviewId);
                
            if (bookReview?.Book != null && bookReview.Book.CoverImageId == null)
            {
                bookReview.Book.CoverImageId = image.Id;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
    
    // Google Custom Search API response models
    public class GoogleCustomSearchResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleCustomSearchItem>? Items { get; set; }
    }
    
    public class GoogleCustomSearchItem
    {
        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }
    
    public class GoogleCustomSearchSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SearchEngineId { get; set; } = string.Empty;
    }
} 