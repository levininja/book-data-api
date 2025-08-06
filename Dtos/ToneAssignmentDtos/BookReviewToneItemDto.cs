namespace BookDataApi.Dtos
{
    public class BookReviewToneItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public List<string> Genres { get; set; } = new List<string>();
        public string? MyReview { get; set; }
        public List<int> AssignedToneIds { get; set; } = new List<int>();
        public List<int> SuggestedToneIds { get; set; } = new List<int>();
    }
}