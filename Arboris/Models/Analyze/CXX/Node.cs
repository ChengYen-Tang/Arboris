namespace Arboris.Models.Analyze.CXX;

public record AddNode(Guid ProjectId, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace, Location? DefineLocation, Location? ImplementationLocation);
public class Node
{
    public Guid ProjectId { get; set; }
    public Guid Id { get; set; }
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public string? NameSpace { get; set; }
    public Location? DefineLocation { get; set; }
    public Location? ImplementationLocation { get; set; }
}

public record NodeInfo(Guid Id, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace);
