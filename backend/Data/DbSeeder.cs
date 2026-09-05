using DevOpsPortfolio.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // ------------------------------------------
        // Prevent duplicate seed
        // ------------------------------------------

        if (await context.Cars.AnyAsync())
            return;

        // ------------------------------------------
        // Car
        // ------------------------------------------

        var car = new Car
        {
            Make = "Audi",
            Model = "A4",
            Year = 2020
        };

        context.Cars.Add(car);

        await context.SaveChangesAsync();

        // ------------------------------------------
        // Search Request
        // ------------------------------------------

        var searchRequest = new SearchRequest
        {
            CarId = car.Id,
            Status = "Completed"
        };

        context.SearchRequests.Add(searchRequest);

        await context.SaveChangesAsync();

        // ------------------------------------------
        // Sources
        // ------------------------------------------

        var sources = new List<Source>
        {
            new Source
            {
                SearchRequestId = searchRequest.Id,
                Url = "https://www.whatcar.com/",
                Title = "Audi A4 Reliability",
                Domain = "whatcar.com"
            },

            new Source
            {
                SearchRequestId = searchRequest.Id,
                Url = "https://www.carwow.co.uk/",
                Title = "Audi A4 Review",
                Domain = "carwow.co.uk"
            }
        };

        context.Sources.AddRange(sources);

        await context.SaveChangesAsync();
    }
}