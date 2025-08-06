namespace book_data_api.Models
{
    /// <summary>
    /// Common interface for tone-related classes.
    /// Two implementations:
    /// - Tone: Database entity with EF navigation properties
    /// - ToneItem: View model with UI-specific properties (ShouldRemove)
    /// Separation reasons: UI form binding vs. database persistence, 
    /// different serialization needs, and clear separation of concerns.
    /// </summary>
    public interface ITone
    {
        int Id { get; set; }
        string Name { get; set; }
        string? Description { get; set; }
        int? ParentId { get; set; }
    }
} 