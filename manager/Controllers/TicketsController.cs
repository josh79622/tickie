using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;
using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Tickie.Manager.Controllers;

[ApiController]
[Route("projects/{projectId}/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly TickieDbContext _dbContext;

    public TicketsController(TickieDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<TicketResponse>>> GetAll(int projectId)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.RemovedAt == null);
        if (project == null)
        {
            return NotFound();
        }

        var phaseIds = await _dbContext.Phases
            .Where(ph => ph.ProjectId == projectId)
            .Select(ph => ph.Id)
            .ToListAsync();


        return await _dbContext.Tickets
            .Where(t => phaseIds.Contains(t.PhaseId))
            .OrderBy(t => t.PlannedStart == null)
            .ThenBy(t => t.PlannedStart)
            .ThenBy(t => t.Id)
            .Select(t => new TicketResponse(
                t.Id,
                t.PhaseId,
                t.Title,
                t.Description,
                t.Type,
                t.Status,
                t.PlannedStart,
                t.EstimatedHours,
                t.AssignedAgent,
                project.BaselineFrozenAt != null && t.CreatedAt > project.BaselineFrozenAt
            ))
            .ToListAsync();
    }
}