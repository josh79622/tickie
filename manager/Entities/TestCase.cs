namespace Tickie.Manager.Entities;

public class TestCase
{
    public int Id { get; private init; }
    public required int TicketId { get; set; }
    public required string Content { get; set; }
    public bool? PassedOnLastRun { get; set; } 
}