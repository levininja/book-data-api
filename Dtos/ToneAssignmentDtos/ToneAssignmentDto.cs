namespace BookDataApi.Dtos
{
    public class ToneAssignmentDto
    {
        public List<BookReviewToneItemDto> BookReviews { get; set; } = new List<BookReviewToneItemDto>();
        public List<BookReviewToneItemDto> BooksWithTones { get; set; } = new List<BookReviewToneItemDto>();
        public List<ToneGroupDto> ToneGroups { get; set; } = new List<ToneGroupDto>();
    }
}