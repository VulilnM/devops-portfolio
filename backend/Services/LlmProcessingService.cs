using System.Threading.Channels;
using DevOpsPortfolio.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Services;

public class LlmProcessingService : BackgroundService
{
    private readonly ChannelReader<int> _channelReader;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LlmProcessingService> _logger;

    public LlmProcessingService(
        ChannelReader<int> channelReader,
        IServiceScopeFactory scopeFactory,
        ILogger<LlmProcessingService> logger)
    {
        _channelReader = channelReader;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        // Svaki job dobija svoj scope
        using var scope = _scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        // Učitaj SearchRequest + povezani Car
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

        // Promeni status
        searchRequest.Status = "Processing";

        await dbContext.SaveChangesAsync(cancellationToken);

        // ==========================================
        // OVDE ĆE IĆI LLM PROCESSING
        // ==========================================

        await ProcessWithLlmAsync(
            searchRequest,
            cancellationToken);

        // Nakon uspešne obrade
        searchRequest.Status = "Completed";
        searchRequest.Summary = $"LLM Processed {searchRequest.Car.Make} {searchRequest.Car.Model} {searchRequest.Car.Year}, ReqId: {searchRequest.Id}";

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SearchRequest {SearchRequestId} completed.",
            searchRequestId);
    }

    private async Task ProcessWithLlmAsync(
        Models.SearchRequest searchRequest,
        CancellationToken cancellationToken)
    {
        var car = searchRequest.Car;

        _logger.LogInformation(
            "Sending {Make} {Model} {Year} to LLM...",
            car.Make,
            car.Model,
            car.Year);

        // TODO:
        // Poziv Ollama / Qwen modela

        await Task.Delay(5000, cancellationToken);
    }
}