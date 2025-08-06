// Used for mapping Goodreads book review CSV columns to properties for import.
using CsvHelper.Configuration.Attributes;

namespace book_data_api.Models
{
    public class GoodreadsBookReviewCsv
    {
        [Name("Book Id")]
        public string? BookId { get; set; }

        [Name("Title")]
        public string? Title { get; set; }

        [Name("Author")]
        public string? Author { get; set; }

        [Name("Author l-f")]
        public string? Author_l_f { get; set; }

        [Name("Additional Authors")]
        public string? Additional_Authors { get; set; }

        [Name("ISBN")]
        public string? ISBN { get; set; }

        [Name("ISBN13")]
        public string? ISBN13 { get; set; }

        [Name("My Rating")]
        public string? My_Rating { get; set; }

        [Name("Average Rating")]
        public string? Average_Rating { get; set; }

        [Name("Publisher")]
        public string? Publisher { get; set; }

        [Name("Binding")]
        public string? Binding { get; set; }

        [Name("Number of Pages")]
        public string? Number_of_Pages { get; set; }

        [Name("Year Published")]
        public string? Year_Published { get; set; }

        [Name("Original Publication Year")]
        public string? Original_Publication_Year { get; set; }

        [Name("Date Read")]
        public string? Date_Read { get; set; }

        [Name("Date Added")]
        public string? Date_Added { get; set; }

        [Name("Bookshelves")]
        public string? Bookshelves { get; set; }

        [Name("Bookshelves with positions")]
        public string? Bookshelves_with_positions { get; set; }

        [Name("Exclusive Shelf")]
        public string? Exclusive_Shelf { get; set; }

        [Name("My Review")]
        public string? My_Review { get; set; }

        [Name("Spoiler")]
        public string? Spoiler { get; set; }

        [Name("Private Notes")]
        public string? Private_Notes { get; set; }

        [Name("Read Count")]
        public string? Read_Count { get; set; }

        [Name("Owned Copies")]
        public string? Owned_Copies { get; set; }

        // Computed properties for easier mapping
        public string AuthorFirstName => Author_l_f?.Split(',')?.Length > 1 ? Author_l_f.Split(',')[1]?.Trim() ?? "" : "";
        public string AuthorLastName => Author_l_f?.Split(',')?.Length > 0 ? Author_l_f.Split(',')[0]?.Trim() ?? "" : "";
        public int MyRating => int.TryParse(My_Rating, out int rating) ? rating : 0;
        public decimal AverageRating => decimal.TryParse(Average_Rating, out decimal avgRating) ? avgRating : 0;
        public int? NumberOfPages => int.TryParse(Number_of_Pages, out int pages) ? pages : null;
        public int? OriginalPublicationYear 
        { 
            get 
            {
                if (string.IsNullOrWhiteSpace(Original_Publication_Year))
                    return null; // Sensible default for blank values
                    
                if (int.TryParse(Original_Publication_Year, out int year))
                    return year; // Can be negative (for BCE dates)
                    
                return null; // Return null if parsing fails
            }
        }
        public DateTime? DateRead 
        { 
            get 
            {
                if (string.IsNullOrWhiteSpace(Date_Read))
                    return null;
                    
                // Try parsing with yyyy/MM/dd format (Goodreads format)
                if (DateTime.TryParseExact(Date_Read, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    return DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    
                // Try parsing with different formats as fallback
                if (DateTime.TryParse(Date_Read, out DateTime date2))
                    return DateTime.SpecifyKind(date2, DateTimeKind.Utc);
                    
                // If we can't parse it, return null instead of MinValue
                return null;
            }
        }
        public string? MyReview => My_Review;
        public string? ISBN10 => ISBN; // Map ISBN to ISBN10
        public string? ISBN13Value => ISBN13; // Rename to avoid conflict with property name
    }
} 