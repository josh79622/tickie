using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Dtos;

namespace Tickie.Manager.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly TickieDbContext _dbContext;
    public ProjectsController(TickieDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<List<ProjectResponse>> GetAll()
    {
        return await _dbContext.Projects
            .Where(p => p.RemovedAt == null)
            .OrderBy(p => p.SortOrder)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.FolderPath,
                p.SortOrder,
                p.BaselineFrozenAt
            ))
            .ToListAsync();
    }
}
