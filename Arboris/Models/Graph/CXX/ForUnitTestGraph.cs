namespace Arboris.Models.Graph.CXX;

public class ForUnitTestGraph
{
    public ForUnitTestNode Node { get; set; } = null!;
    public OverViewNode[] NodeMembers { get; set; } = null!;
    public OverViewNode[] NodeTypes { get; set; } = null!;
    public OverViewNode[] NodeDependencies { get; set; } = null!;
}

public class ForUnitTestNode : OverViewNode
{
    public string? ExampleCode { get; set; }
}
