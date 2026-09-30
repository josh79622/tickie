namespace Tickie.Manager.Dtos;

public record PhaseResponse(
    int Id,
    int ProjectId,
    string Name,
    int Order
);