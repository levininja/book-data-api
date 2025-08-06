namespace BookDataApi.Dtos
{
    public class ToneGroupDto
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string ColorClass { get; set; } = "";
        public List<ToneDisplayItemDto> Tones { get; set; } = new List<ToneDisplayItemDto>();
    }
}