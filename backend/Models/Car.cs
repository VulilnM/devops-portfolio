namespace DevOpsPortfolio.Backend.Models;

public class Car
{
    public int Id { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 1 : N
    public ICollection<SearchRequest> SearchRequests { get; set; }
        = new List<SearchRequest>();
}