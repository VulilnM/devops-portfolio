using DevOpsPortfolio.Backend.Data;
using DevOpsPortfolio.Backend.Dto;
using DevOpsPortfolio.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CarsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzeCarResponse>> Analyze([FromBody] AnalyzeCarRequest request)
    {
        var car = new Car
        {
            Make = request.Make,
            Model = request.Model,
            Year = request.Year
        };

        _context.Cars.Add(car);

        var searchRequest = new SearchRequest
        {
            Car = car,
            Status = "Pending"
        };

        _context.SearchRequests.Add(searchRequest);

        await _context.SaveChangesAsync();

        var response = new AnalyzeCarResponse
        {
            SearchRequestId = searchRequest.Id,
            Status = searchRequest.Status
        };

        return Accepted(response);
    }
}