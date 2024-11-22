namespace Arboris.Models.Analyze.CXX;

public record AddNode(Guid ProjectId, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace, Location? DefineLocation, Location? ImplementationLocation);
public class Node
{
    public Guid ProjectId { get; set; }
    public Guid Id { get; set; }
    public string VcProjectName { get; set; } = null!;
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public string? NameSpace { get; set; }
    public Location? DefineLocation { get; set; }
    public Location? ImplementationLocation { get; set; }
    public IReadOnlySet<string>? IncludeStrings { get; set; }
}

public record NodeInfo(Guid Id, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace, string? UserDescription, string? LLMDescription);
public record NodeInfoWithLocation(Guid Id, string VcProjectName, string? CursorKindSpelling, string? Spelling, string? CxType, string? NameSpace, string? UserDescription, string? LLMDescription, IReadOnlySet<string>? IncludeStrings, Location? DefineLocation, Location? ImplementationLocation)
    : NodeInfo(Id, VcProjectName, CursorKindSpelling, Spelling, CxType, NameSpace, UserDescription, LLMDescription);
public record NodeWithLocationDto(Guid NodeId, Location? DefineLocation, Location? ImplementationLocation);
