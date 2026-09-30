using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Data;
using Tickie.Manager.Dtos;

namespace Tickie.Manager.Controllers;

[ApiController]
[Route("projects/{projectId}/[controller]")]
public class PhasesController : ControllerBase
{
    private readonly TickieDbContext _dbContext;

    public PhasesController(TickieDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<PhaseResponse>>> GetAll(int projectId)
    {
        var projectExists = await _dbContext.Projects
            .AnyAsync(p => p.Id == projectId && p.RemovedAt == null);
        if (!projectExists)
        {
            return NotFound();
        }

        return await _dbContext.Phases
            .Where(ph => ph.ProjectId == projectId)
            .OrderBy(ph => ph.Order)
            .Select(ph => new PhaseResponse(
                ph.Id,
                ph.ProjectId,
                ph.Name,
                ph.Order
            ))
            .ToListAsync();
    }
}