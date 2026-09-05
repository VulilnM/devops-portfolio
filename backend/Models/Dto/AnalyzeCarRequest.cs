namespace DevOpsPortfolio.Backend.Dto;

public class AnalyzeCarRequest
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
}