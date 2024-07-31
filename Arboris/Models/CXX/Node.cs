namespace Arboris.Models.CXX;

public record AddNode(Guid ProjectId, string? CursorKindSpelling, string? Spelling, string? CxType, Location? DefineLocation, Location? ImplementationLocation);
public class Node
{
    public Guid ProjectId { get; set; }
    public Guid Id { get; set; }
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public Location? DefineLocation { get; set; }
    public Location? ImplementationLocation { get; set; }
}
