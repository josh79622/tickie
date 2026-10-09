using System;
using Tickie.Manager.Entities;

namespace Tickie.Manager.Dtos;

public record CreateTicketRequest(
    int PhaseId,
    string Title,
    string Description,
    TicketType Type,
    DateTime? PlannedStart,
    double? EstimatedHours,
    string? AssignedAgent = null,
    List<int>? PrerequisiteTicketIds = null,
    List<string>? Labels = null
);
