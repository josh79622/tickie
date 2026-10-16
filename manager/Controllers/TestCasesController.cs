using Tickie.Manager.Dtos;
using Tickie.Manager.Entities;
using Tickie.Manager.Data;
using Tickie.Manager.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Tickie.Manager.Controllers;

[ApiController]
[Route("tickets/{ticketId}/testcases")]
public class TestCasesController : ControllerBase
{
    private readonly TickieDbContext _dbContext;
    public TestCasesController (TickieDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<TestCaseResponse>>> GetAll(int ticketId)
    {
        var ticketExists = await _dbContext.Tickets
            .AnyAsync(t => t.Id == ticketId);
        
        if (!ticketExists)
        {
            return NotFound(new { message = "Ticket not found."});
        }


        return await _dbContext.TestCases
            .Where(tc => tc.TicketId == ticketId)
            .OrderBy(tc => tc.Id)
            .Select(tc => new TestCaseResponse(
                tc.Id,
                tc.TicketId,
                tc.Content,
                tc.PassedOnLastRun
            ))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<TestCaseResponse>> Create(int ticketId, CreateTestCaseRequest request)
    {
        var ticket = await _dbContext.Tickets
            .FindAsync(ticketId);

        if (ticket == null)
        {
            return NotFound(new { message = "Ticket not found."});
        }

        if (ticket.Type == TicketType.Design)
        {
            return BadRequest(new { message = "Design tickets don't have test cases." });
        }

        if (ticket.Status != TicketStatus.TestCases)
        {
            return Conflict(new { message = $"Test cases are locked while the ticket is in {ticket.Status}." });
        }

                var testCase = new TestCase
        {
            TicketId = ticket.Id,
            Content = request.Content
        };

        _dbContext.TestCases.Add(testCase);
        await _dbContext.SaveChangesAsync();

        return StatusCode(201, new TestCaseResponse(
            testCase.Id,
            testCase.TicketId,
            testCase.Content,
            testCase.PassedOnLastRun
        ));
    }
}
