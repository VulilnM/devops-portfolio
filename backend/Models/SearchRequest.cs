namespace DevOpsPortfolio.Backend.Models;

public class SearchRequest
{
    public int Id { get; set; }

    public int CarId { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // N : 1
    public Car Car { get; set; } = null!;

    // 1 : N
    public ICollection<Source> Sources { get; set; }
        = new List<Source>();
}