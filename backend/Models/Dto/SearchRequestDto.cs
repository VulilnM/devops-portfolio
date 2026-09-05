namespace DevOpsPortfolio.Backend.Dto;

public class SearchRequestDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CarDto Car { get; set; } = null!;
    public List<SourceDto> Sources { get; set; } = new();
}