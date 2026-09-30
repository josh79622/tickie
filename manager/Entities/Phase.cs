namespace Tickie.Manager.Entities;

public class Phase
{
    public int Id { get; private init; }
    public required int ProjectId { get; set; }
    public required string Name { get; set; }
    public required int Order { get; set; }
}