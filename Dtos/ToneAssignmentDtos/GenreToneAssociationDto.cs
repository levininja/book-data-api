namespace BookDataApi.Dtos
{
    public class GenreToneAssociationDto
    {
        public string Genre { get; set; } = "";
        public List<string> Tones { get; set; } = new List<string>();
    }
}