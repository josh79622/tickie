namespace Tickie.Manager.Dtos;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    string FolderPath,
    int SortOrder,
    DateTime? BaselineFrozenAt
);
