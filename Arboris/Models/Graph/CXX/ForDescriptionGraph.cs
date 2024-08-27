namespace Arboris.Models.Graph.CXX;

public class ForDescriptionGraph
{
    public ForDescriptionNode Node { get; set; } = null!;
    public OverViewNode[] NodeMembers { get; set; } = null!;
    public OverViewNode[] NodeTypes { get; set; } = null!;
    public OverViewNode[] NodeDependencies { get; set; } = null!;
}

public class ForDescriptionNode
{
    public string? SourceCode { get; set; }
    public string? UserDescription { get; set; }
}
