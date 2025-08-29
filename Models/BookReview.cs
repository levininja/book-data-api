using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace book_data_api.Models
{
    public class BookReview
    {
        public int Id { get; set; }
        public required int ReviewerRating { get; set; } // 1-5 (renamed from MyRating)
        public required string ReviewerFullName { get; set; } = "Levi Hobbs";
        public DateTime? DateRead { get; set; }
        public string? Review { get; set; } // renamed from MyReview
        
        // Foreign key to Book
        public int BookId { get; set; }
        [JsonIgnore]
        public Models.Book Book { get; set; } = null!; // Navigation property to Book
        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public bool HasReviewContent { get; set; }

        [NotMapped]
        public string ReviewPreviewText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Review))
                    return string.Empty;
                    
                /* Strip all HTML except <b>, </b>, <i>, </i> tags
                   Regex breakdown:
                   < - Match opening angle bracket
                   (?!/?[bi]>) - Negative lookahead that fails to match if what follows is:
                     /? - Optional forward slash (for closing tags)
                     [bi] - Either 'b' or 'i' character
                     > - Closing angle bracket
                   [^>]* - Match any characters that aren't > zero or more times
                   > - Match closing angle bracket */
                string cleanText = Regex.Replace(Review, @"<(?!/?[bi]>)[^>]*>", "");
                
                // Get first 300 characters
                string preview = cleanText.Length <= 300 ? cleanText : cleanText.Substring(0, 300);
                
                // Add "Read More" link if text was truncated
                if (cleanText.Length > 300)
                {
                    // Find the last complete word to avoid cutting off mid-word
                    int lastSpace = preview.LastIndexOf(' ');
                    if (lastSpace > 250) // Only trim if we're not cutting too much
                    {
                        preview = preview.Substring(0, lastSpace);
                    }
                    
                    // TODO: implement these links later (are broken for now)
                    preview += $"... <a href=\"/read/reviews/{Id}\">Read More</a>";
                }
                
                return preview;
            }
        }
        // Read-only property for estimated how many minutes it takes to read the review
        [NotMapped]
        public int ReadingTimeMinutes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Review))
                    return 0;

                /* Strip all HTML tags to get plain text for word count
                   Regex breakdown:
                   < - Match opening angle bracket
                   [^>]* - Match any characters that aren't > zero or more times
                   > - Match closing angle bracket
                   
                   This removes both opening and closing tags like <p>, </p>, <br>, etc.
                   leaving only the text content */
                string plainText = Regex.Replace(Review, @"<[^>]*>", "");
                
                // Count words (split by whitespace and filter out empty entries)
                int wordCount = plainText.Split(new char[] { ' ', '\t', '\n', '\r' }, 
                    StringSplitOptions.RemoveEmptyEntries).Length;
                
                // Calculate reading time: 250 words per minute, minimum 1 minute
                int minutes = Math.Max(1, (int)Math.Ceiling(wordCount / 250.0));
                
                return minutes;
            }
        }
    }
} 