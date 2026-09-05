namespace DevOpsPortfolio.Backend.Dto;

public class SourceDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Domain { get; set; }
    public DateTime FetchedAt { get; set; }
}