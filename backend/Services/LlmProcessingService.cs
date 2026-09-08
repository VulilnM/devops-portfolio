using System.Threading.Channels;
using DevOpsPortfolio.Backend.Data;
using DevOpsPortfolio.Backend.Helpers;
using DevOpsPortfolio.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Services;

public class LlmProcessingService : BackgroundService
{
    private readonly ChannelReader<int> _channelReader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LlmProcessingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmProcessingService(
        ChannelReader<int> channelReader,
        IServiceScopeFactory scopeFactory,
        ILogger<LlmProcessingService> logger,
        IHttpClientFactory httpClientFactory
        )

    {
        _channelReader = channelReader;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("LLM Processing Service started.");

        await foreach (var searchRequestId
            in _channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSearchRequestAsync(
                    searchRequestId,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing SearchRequest {SearchRequestId}",
                    searchRequestId);
            }
        }

        _logger.LogInformation("LLM Processing Service stopped.");
    }

    private async Task ProcessSearchRequestAsync(
        int searchRequestId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing SearchRequest {SearchRequestId}",
            searchRequestId);

        // Each job gets its own scope
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Load SearchRequest + related Car
        var searchRequest = await dbContext.SearchRequests
            .Include(x => x.Car)
            .FirstOrDefaultAsync(
                x => x.Id == searchRequestId,
                cancellationToken);

        if (searchRequest == null)
        {
            _logger.LogWarning(
                "SearchRequest {SearchRequestId} was not found.",
                searchRequestId);

            return;
        }

        _logger.LogInformation(
            "Found SearchRequest {SearchRequestId} for {Make} {Model} {Year}",
            searchRequest.Id,
            searchRequest.Car.Make,
            searchRequest.Car.Model,
            searchRequest.Car.Year);

        // Change status
        searchRequest.Status = "Processing...";

        await dbContext.SaveChangesAsync(cancellationToken);

        searchRequest.Status = "Web scraping in progress...";
        await dbContext.SaveChangesAsync(cancellationToken);
        var webSearchingHelper = new WebSearchingHelper(_httpClientFactory);
        var searchResults = await webSearchingHelper.SearchCarAsync(
            searchRequest,
            cancellationToken);
        var extractedSources = webSearchingHelper.ExtractSources(searchResults);

        searchRequest.Status = "LLM processing in progress...";
        await dbContext.SaveChangesAsync(cancellationToken);
        string llmResult = await ProcessWithLlmAsync(
            searchRequest,
            searchResults,
            cancellationToken);

        foreach (var (url, title) in extractedSources)
        {
            string? domain = null;
            try { domain = new Uri(url).Host; } catch { /* ignore invalid URL */ }

            dbContext.Sources.Add(new Source
            {
                SearchRequestId = searchRequest.Id,
                Url = url,
                Title = title,
                Domain = domain,
                FetchedAt = DateTime.UtcNow
            });
        }

        searchRequest.Status = "Completed";
        searchRequest.Summary = llmResult;

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SearchRequest {SearchRequestId} completed.",
            searchRequestId);
    }

    private async Task<string> ProcessWithLlmAsync(
        Models.SearchRequest searchRequest,
        string searchResults,
        CancellationToken cancellationToken)
    {
        var client =
            _httpClientFactory.CreateClient("Ollama");

        var prompt = $"""
            You are an automotive analyst.

            Analyze this car:

            Make: {searchRequest.Car.Make}
            Model: {searchRequest.Car.Model}
            Year: {searchRequest.Car.Year}

            Here are internet search results:

            {searchResults}

            Based on the provided information, give a concise analysis covering:

            1. General overview
            2. Engine and performance
                - Make sure to list out all fuel types available for this car and their respective engine sizes (inlcuding hybrid or full electric).
            3. Reliability
            4. Common problems
            5. Overall recommendation
            6. Maintenance cost and tips
            7. Experiences from owners
            Do not invent information.
            """;

        var request = new
        {
            model = "qwen2.5",
            prompt = prompt,
            stream = false
        };

        _logger.LogInformation(
            "Sending SearchRequest {SearchRequestId} to Ollama.",
            searchRequest.Id);

        var response = await client.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<OllamaResponse>(
                    cancellationToken);

        return result?.Response ?? string.Empty;
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}
