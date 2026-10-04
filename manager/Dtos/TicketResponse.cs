using Tickie.Manager.Entities;

namespace Tickie.Manager.Dtos;

public record TicketResponse(
    int Id, 
    int PhaseId, 
    string Title, 
    string Description, 
    TicketType Type,
    TicketStatus Status,
    DateTime? PlannedStart,
    double? EstimatedHours,
    string AssignedAgent,
    bool IsUnplanned
);
