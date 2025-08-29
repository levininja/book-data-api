using BookDataApi.Shared.Models;

namespace book_data_api.Models
{
    public class Tone : BookDataApi.Shared.Models.Tone
    {
        // Navigation property for self-referencing relationship (parent tone)
        public Tone? Parent { get; set; }
        
        // Navigation property for child tones
        public ICollection<Tone> Subtones { get; set; } = new List<Tone>();
        
        // Navigation property for many-to-many relationship with Book
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
} 