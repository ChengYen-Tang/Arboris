namespace Arboris.Models.Graph.CXX;

public class OverallGraph
{
    public OverallNode[] Nodes { get; set; }
    public OverallNodeMember[] NodeMembers { get; set; }
    public OverallNodeType[] NodeTypes { get; set; }
    public OverallNodeDependency[] NodeDependencies { get; set; }
}

public class OverallNode
{
    public Guid Id { get; set; }
    public string? CursorKindSpelling { get; set; }
}

public class OverallNodeMember
{
    public Guid NodeId { get; set; }

    public Guid MemberId { get; set; }
}

public class OverallNodeType
{
    public Guid NodeId { get; set; }

    public Guid TypeId { get; set; }
}

public class OverallNodeDependency
{
    public Guid NodeId { get; set; }

    public Guid FromId { get; set; }
}
