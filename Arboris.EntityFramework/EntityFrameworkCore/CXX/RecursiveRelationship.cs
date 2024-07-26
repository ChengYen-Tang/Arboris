namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

public class NodeMember
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid MemberId { get; set; }
    public Node Member { get; set; }
}

public class NodeType
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid TypeId { get; set; }
    public Node Type { get; set; }
}

public class Dependency
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid FromId { get; set; }
    public Node From { get; set; }
}
