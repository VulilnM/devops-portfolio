namespace DevOpsPortfolio.Backend.Models;

public class Source
{
    public int Id { get; set; }

    public int SearchRequestId { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Domain { get; set; }

    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    // N : 1
    public SearchRequest SearchRequest { get; set; } = null!;
}