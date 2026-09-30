namespace Tickie.Manager.Entities;

public class Project
{
    public int Id { get; private init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string FolderPath { get; set; }
    public required int SortOrder { get; set; }
    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;
    public DateTime? BaselineFrozenAt { get; set; }
    public DateTime? RemovedAt { get; set; }
}