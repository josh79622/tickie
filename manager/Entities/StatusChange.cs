namespace Tickie.Manager.Entities;

public class StatusChange
{
    public int Id { get; private init; }
    public required int TicketId { get; set; }
    public required TicketStatus FromStatus { get; set; }
    public required TicketStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; private init; } = DateTime.UtcNow;
    public string? Note { get; set; }
}