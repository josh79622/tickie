namespace Tickie.Manager.Entities;

public class TicketDependency
{
    public required int TicketId { get; set; }
    public required int PrerequisiteTicketId { get; set; }
}