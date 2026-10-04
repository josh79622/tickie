using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Data;
using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;

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

    [HttpPost]
    public async Task<ActionResult<PhaseResponse>> Create(int projectId, CreatePhaseRequest request)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.RemovedAt == null);
        if (project == null)
        {
            return NotFound();
        }

        var existingPhase = await _dbContext.Phases
            .FirstOrDefaultAsync(ph => ph.Name == request.Name && ph.ProjectId == projectId);
        
        if (existingPhase != null)
        {
            return Conflict(new { message = "A phase with the same name already exists." });
        }

        var theLastPhaseOrder = await _dbContext.Phases
            .Where(ph => ph.ProjectId == projectId)
            .OrderByDescending(ph => ph.Order)
            .Select(ph => ph.Order)
            .FirstOrDefaultAsync();

        var newPhase = new Phase
        {
            ProjectId = projectId,
            Name = request.Name,
            Order = theLastPhaseOrder + 1
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        _dbContext.Phases.Add(newPhase);
        await _dbContext.SaveChangesAsync();

        var newTicket = new Ticket
        {
            PhaseId = newPhase.Id,
            Type = TicketType.QA,
            Title = $"QA {newPhase.Name}",
            Description = request.QaDescription ?? $"End-to-end tests for {newPhase.Name}.",
            AssignedAgent = project.DefaultAgent,
        };

        _dbContext.Tickets.Add(newTicket);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return StatusCode(201, new PhaseResponse(
            newPhase.Id,
            newPhase.ProjectId,
            newPhase.Name,
            newPhase.Order
        ));
        
    }

    [HttpPatch("reorder")]
    public async Task<ActionResult> Reorder(int projectId, ReorderPhasesRequest request)
    {
        var projectExists = await _dbContext.Projects
            .AnyAsync(p => p.Id == projectId && p.RemovedAt == null);
        if (!projectExists)
        {
            return NotFound();
        }

        var phases = await _dbContext.Phases
            .Where(ph => ph.ProjectId == projectId)
            .ToListAsync();

        var phaseIdsSet = phases.Select(ph => ph.Id).ToHashSet();

        if (request.PhaseIds.Count != phaseIdsSet.Count || !phaseIdsSet.SetEquals(request.PhaseIds))
        {
            return BadRequest(new { message = "The provided phase IDs do not match the existing phases." });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // Pass 1: move every phase to a temporary negative order so pass 2 can't collide.
        for (var i = 0; i < request.PhaseIds.Count; i++)
        {
            var phase = phases.First(ph => ph.Id == request.PhaseIds[i]);
            phase.Order = -(i + 1);
        }
        await _dbContext.SaveChangesAsync();

        // Pass 2: the real orders.
        for (var i = 0; i < request.PhaseIds.Count; i++)
        {
            var phase = phases.First(ph => ph.Id == request.PhaseIds[i]);
            phase.Order = i + 1;
        }
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
        return NoContent();
    }
}