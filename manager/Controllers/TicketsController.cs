using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;
using Tickie.Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Linq;

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

        var tickets = await _dbContext.Tickets
            .Where(t => phaseIds.Contains(t.PhaseId))
            .OrderBy(t => t.PlannedStart == null)
            .ThenBy(t => t.PlannedStart)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var ticketIds = tickets.Select(t => t.Id).ToList();
        var labelRows = await _dbContext.TicketLabels
            .Where(tl => ticketIds.Contains(tl.TicketId))
            .Join(_dbContext.Labels,
                tl => tl.LabelId,
                l => l.Id,
                (tl, l) => new { tl.TicketId, l.Name })
            .ToArrayAsync();

        return tickets
            .Select(t => ToResponse(
                t, 
                project.BaselineFrozenAt, 
                labelRows.Where(r => r.TicketId == t.Id).Select(r => r.Name).OrderBy(name => name).ToList()
            ))
            .ToList();
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

        var prerequisiteIds = request.PrerequisiteTicketIds?.Distinct().ToList() ?? new List<int>();
        if (prerequisiteIds.Count > 0)
        {
            var phaseIds = await _dbContext.Phases
                .Where(ph => ph.ProjectId == projectId)
                .Select(ph => ph.Id)
                .ToListAsync();

            var foundCount = await _dbContext.Tickets
                .CountAsync(t => prerequisiteIds.Contains(t.Id) && phaseIds.Contains(t.PhaseId));
            
            if (foundCount != prerequisiteIds.Count)
            {
                return BadRequest(new { message = "Prerequisites must be tickets in this project." });
            }
        }

        var labelNames = request.Labels?
            .Select(name => name.Trim().ToLowerInvariant())
            .Where(name => name != "")
            .Distinct()
            .ToList() ?? new List<string>();

        var qaTicket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Type == TicketType.QA && t.PhaseId == request.PhaseId);

        var qaReopened = false;
        if (qaTicket != null && qaTicket.Status == TicketStatus.Done)
        {
            qaTicket.Status = TicketStatus.WaitingForDev;
            qaReopened = true;
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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        var existingLabels = await _dbContext.Labels
            .Where(l => l.ProjectId == projectId && labelNames.Contains(l.Name.ToLower()))
            .ToListAsync();

        var existingLabelNames = existingLabels
            .Select(l => l.Name).ToList();

        var newLabels = labelNames
            .Where(name => !existingLabelNames.Contains(name))
            .Select(name => new Label { ProjectId = projectId, Name = name})
            .ToList();

        _dbContext.Labels.AddRange(newLabels);
        await _dbContext.SaveChangesAsync();

        foreach (var label in existingLabels.Concat(newLabels))
        {
            _dbContext.TicketLabels.Add(new TicketLabel
            {
                TicketId = ticket.Id,
                LabelId = label.Id
            });
        }
        await _dbContext.SaveChangesAsync();

        foreach (var prerequisiteId in prerequisiteIds)
        {
            _dbContext.TicketDependencies.Add(new TicketDependency
            {
                TicketId = ticket.Id,
                PrerequisiteTicketId = prerequisiteId
            });
        }
        await _dbContext.SaveChangesAsync();

        if (qaReopened && qaTicket != null)
        {
            var statusChange = new StatusChange
            {
                TicketId = qaTicket.Id,
                FromStatus = TicketStatus.Done,
                ToStatus = TicketStatus.WaitingForDev
            };

            _dbContext.StatusChanges.Add(statusChange);
            await _dbContext.SaveChangesAsync();

            _dbContext.StatusChangeTickets.Add(new StatusChangeTicket
            {
                StatusChangeId = statusChange.Id,
                TicketId = ticket.Id
            });
            await _dbContext.SaveChangesAsync();
        }

        var ticketLabelNames = existingLabels.Concat(newLabels)
            .Select(l => l.Name)
            .OrderBy(name => name)
            .ToList();

        await transaction.CommitAsync();
        return StatusCode(201, ToResponse(ticket, project.BaselineFrozenAt, ticketLabelNames));
    }


    private static TicketResponse ToResponse(Ticket t, DateTime? baselineFrozenAt, List<string> labels)
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
            baselineFrozenAt != null && t.CreatedAt > baselineFrozenAt,
            labels
        );
    }
}