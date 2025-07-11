namespace Arboris.Models.Analyze.CXX;

public record AddNode(Guid ProjectId, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace, string? AccessSpecifiers, Location? DefineLocation, Location? ImplementationLocation);
public class Node
{
    public Guid ProjectId { get; set; }
    public Guid Id { get; set; }
    public string VcProjectName { get; set; } = null!;
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public string? NameSpace { get; set; }
    public string? AccessSpecifiers { get; set; }
    public Location? DefineLocation { get; set; }
    public ICollection<Location> ImplementationsLocation { get; set; }
}

public record NodeInfo(Guid Id, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? AccessSpecifiers, string? NameSpace, string? UserDescription, string? LLMDescription);
public record NodeInfoWithLocation(Guid Id, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? AccessSpecifiers, string? NameSpace, string? UserDescription, string? LLMDescription, Location? DefineLocation, ICollection<Location> ImplementationsLocation)
    : NodeInfo(Id, VcProjectName, CursorKindSpelling, Spelling, CxType, AccessSpecifiers, NameSpace, UserDescription, LLMDescription);
public record NodeWithLocationDto(Guid NodeId, Location? DefineLocation, ICollection<Location> ImplementationsLocation);
