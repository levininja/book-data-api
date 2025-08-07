namespace BookDataApi.Dtos
{
    public class ToneAssignmentDto
    {
        public List<BookReviewToneItemDto> BookReviews { get; set; } = new List<BookReviewToneItemDto>();
        public List<BookReviewToneItemDto> BooksWithTones { get; set; } = new List<BookReviewToneItemDto>();
        public List<ToneItemDto> Tones { get; set; } = new List<ToneItemDto>();
    }
}