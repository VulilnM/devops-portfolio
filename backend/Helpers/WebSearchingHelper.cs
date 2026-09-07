using DevOpsPortfolio.Backend.Models;

namespace DevOpsPortfolio.Backend.Helpers;

public class WebSearchingHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebSearchingHelper> _logger;

    public WebSearchingHelper(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<WebSearchingHelper>();
    }

    public async Task<string> SearchCarAsync(SearchRequest searchRequest, CancellationToken cancellationToken)
    {
        var client =
            _httpClientFactory.CreateClient("SearXNG");

        var query =
            $"{searchRequest.Car.Make} {searchRequest.Car.Model} {searchRequest.Car.Year} " +
            "reliability, common problems, buyers guide, maintenance, issues, owner experiences or reviews, known problems";

        var url =
            $"/search?q={Uri.EscapeDataString(query)}&format=json";

        _logger.LogInformation(
            "Searching SearXNG for: {Query}",
            query);

        var response = await client.GetAsync(
            url,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadAsStringAsync(cancellationToken);
    }
}
