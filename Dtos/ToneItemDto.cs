namespace BookDataApi.Dtos
{
    public class ToneItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public bool ShouldRemove { get; set; }
        public List<ToneItemDto> Subtones { get; set; } = new List<ToneItemDto>();
    }
}