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
        var existingProject = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.FolderPath == request.FolderPath);
        
        var smallestSortOrder = await _dbContext.Projects
            .Where(p => p.RemovedAt == null)
            .MinAsync(p => (int?)p.SortOrder);

        var topSortOrder = (smallestSortOrder ?? 1) - 1;

        if (existingProject != null && existingProject.RemovedAt == null)
        {
            return Conflict(new { message = "The folder is already a project." });
        } 
        else if (existingProject != null && existingProject.RemovedAt != null)
        {
            existingProject.RemovedAt = null;
            existingProject.SortOrder = topSortOrder;
            await _dbContext.SaveChangesAsync();
            return Ok(ToResponse(existingProject));
        }

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            FolderPath = request.FolderPath,
            SortOrder = topSortOrder
        };

        _dbContext.Projects.Add(project);
        
        await _dbContext.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(project));
    }

    [HttpPatch("{id}/remove")]
    public async Task<ActionResult> Remove(int id)
    {
        var project = await _dbContext.Projects.FindAsync(id);
        if (project == null || project.RemovedAt != null)
        {
            return NotFound();
        }

        project.RemovedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("reorder")]
    public async Task<ActionResult> Reorder(ReorderProjectsRequest request)
    {
        var projects = await _dbContext.Projects
            .Where(p => p.RemovedAt == null)
            .ToListAsync();
        
        var activeIds = projects.Select(p => p.Id).ToHashSet();

        if (request.ProjectIds.Count != projects.Count || !activeIds.SetEquals(request.ProjectIds))
        {
            return BadRequest(new { message = "The list must contain every project on the home page exactly once." });
        }

        for (var i = 0; i < request.ProjectIds.Count; i++)
        {
            var project = projects.First(p => p.Id == request.ProjectIds[i]);
            project.SortOrder = i + 1;
        }

        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private static ProjectResponse ToResponse(Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.FolderPath,
            project.SortOrder,
            project.BaselineFrozenAt
        );
    }
}
