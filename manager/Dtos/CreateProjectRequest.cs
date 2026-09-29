namespace Tickie.Manager.Dtos;

public record CreateProjectRequest(
    string Name,
    string? Description,
    string FolderPath
);
