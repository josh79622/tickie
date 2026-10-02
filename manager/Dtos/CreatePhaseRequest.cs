namespace Tickie.Manager.Dtos;

public record CreatePhaseRequest(
    string Name,
    string? QaDescription = null
);
