namespace Frontend.Dto;

public class AnalyzeCarRequest
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class AnalyzeCarResponse
{
    public int SearchRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CarDto
{
    public int Id { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class SourceDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Domain { get; set; }
    public DateTime FetchedAt { get; set; }
}

public class SearchRequestDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CarDto Car { get; set; } = null!;
    public List<SourceDto> Sources { get; set; } = new();
}