namespace Tickie.Manager.Dtos;

public record TestCaseResponse(
    int Id,
    int TicketId,
    string Content,
    bool? PassedOnLastRun
);
