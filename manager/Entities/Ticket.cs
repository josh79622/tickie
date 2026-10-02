namespace Tickie.Manager.Entities;

public class Ticket
{
    public int Id { get; private init; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required int PhaseId { get; set; }
    public required TicketType Type { get; set; }
    public DateTime? PlannedStart { get; set; }
    public double? EstimatedHours { get; set; }
    public required string AssignedAgent { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Todo;
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

}