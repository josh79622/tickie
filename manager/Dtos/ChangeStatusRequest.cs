using Tickie.Manager.Entities;

namespace Tickie.Manager.Dtos;

public record ChangeStatusRequest(
    TicketStatus ToStatus,
    string? Note = null
);