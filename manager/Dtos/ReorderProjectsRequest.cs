namespace Tickie.Manager.Dtos;

public record ReorderProjectsRequest(
    List<int> ProjectIds
);