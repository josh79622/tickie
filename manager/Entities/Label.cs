namespace Tickie.Manager.Entities;

public class Label
{
    public int Id { get; private init; }
    public required int ProjectId { get; set; }
    public required string Name { get; set; }
}