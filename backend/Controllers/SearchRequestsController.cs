using DevOpsPortfolio.Backend.Data;
using DevOpsPortfolio.Backend.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevOpsPortfolio.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchRequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SearchRequestDto>> GetById(int id)
    {
        var searchRequest = await _context.SearchRequests
            .Include(sr => sr.Car)
            .Include(sr => sr.Sources)
            .FirstOrDefaultAsync(sr => sr.Id == id);

        if (searchRequest is null)
        {
            return NotFound();
        }

        var dto = new SearchRequestDto
        {
            Id = searchRequest.Id,
            Status = searchRequest.Status,
            CreatedAt = searchRequest.CreatedAt,
            Car = new CarDto
            {
                Id = searchRequest.Car.Id,
                Make = searchRequest.Car.Make,
                Model = searchRequest.Car.Model,
                Year = searchRequest.Car.Year
            },
            Sources = searchRequest.Sources.Select(s => new SourceDto
            {
                Id = s.Id,
                Url = s.Url,
                Title = s.Title,
                Domain = s.Domain,
                FetchedAt = s.FetchedAt
            }).ToList()
        };

        return Ok(dto);
    }
}