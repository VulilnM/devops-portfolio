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
    You are an automotive analyst specializing in detailed, model-year-specific vehicle assessments.

    Analyze this car:

    Make: {searchRequest.Car.Make}
    Model: {searchRequest.Car.Model}
    Year: {searchRequest.Car.Year}

    Here are internet search results:

    {searchResults}

    IMPORTANT: Focus exclusively on the {searchRequest.Car.Year} model year. Do not describe other
    model years, generations, or facelifts unless directly relevant to explain a change that applies
    specifically to {searchRequest.Car.Year} (e.g. a mid-cycle update introduced that year). If the
    search results contain information about other years, ignore it or explicitly note that it does
    not apply to this model year.

    Based on the provided information, give a technical, precise analysis covering:

    1. General overview
    2. Engine and performance
        - List every powertrain/fuel type available specifically for the {searchRequest.Car.Year}
          model year (including hybrid or full electric variants), each with exact engine
          displacement, cylinder configuration, horsepower, torque, transmission type, and
          drivetrain (FWD/RWD/AWD).
    3. Reliability
        - Cite specific known issues with part names or systems where possible (e.g. "timing chain
          tensioner", "DPF clogging"), not just general statements.
    4. Common problems
        - Include approximate mileage or age at which issues typically appear, if the search results
          mention it.
    5. Overall recommendation
    6. Maintenance cost and tips
        - Include concrete service intervals or cost ranges where the search results provide them.
    7. Experiences from owners

    Use precise technical terminology throughout. Do not invent information — if a specific figure
    or fact is not present in the search results, state that it is not available rather than
    estimating or guessing.
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
