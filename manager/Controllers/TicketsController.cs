using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;
using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

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
            .Select(t => ToResponse(t, project.BaselineFrozenAt))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(int projectId, CreateTicketRequest request)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.RemovedAt == null);
        if (project == null)
        {
            return NotFound();
        }

        var phaseExists = await _dbContext.Phases
            .AnyAsync(ph => ph.Id == request.PhaseId && ph.ProjectId == projectId);
        
        if (!phaseExists)
        {
            return NotFound(new { message = "Phase not found in this project."});
        }

        if (request.Type == TicketType.QA)
        {
            return BadRequest(new { message = "QA tickets are created with their phase." });
        }

        var ticket = new Ticket
        {
            PhaseId = request.PhaseId,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            PlannedStart = request.PlannedStart,
            EstimatedHours = request.EstimatedHours,
            AssignedAgent = request.AssignedAgent ?? project.DefaultAgent
        };

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();
        
        return StatusCode(201, ToResponse(ticket, project.BaselineFrozenAt));
    }


    private static TicketResponse ToResponse(Ticket t, DateTime? baselineFrozenAt)
    {
        return new TicketResponse(
            t.Id,
            t.PhaseId,
            t.Title,
            t.Description,
            t.Type,
            t.Status,
            t.PlannedStart,
            t.EstimatedHours,
            t.AssignedAgent,
            baselineFrozenAt != null && t.CreatedAt > baselineFrozenAt
        );
    }
}