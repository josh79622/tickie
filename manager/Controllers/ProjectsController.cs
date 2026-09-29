using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;

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

    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request)
    {
        var smallestSortOrder = await _dbContext.Projects
            .Where(p => p.RemovedAt == null)
            .MinAsync(p => (int?)p.SortOrder);

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            FolderPath = request.FolderPath,
            SortOrder = (smallestSortOrder ?? 1) - 1
        };

        _dbContext.Projects.Add(project);
        
        await _dbContext.SaveChangesAsync();

        var response = new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.FolderPath,
            project.SortOrder,
            project.BaselineFrozenAt
        );

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
