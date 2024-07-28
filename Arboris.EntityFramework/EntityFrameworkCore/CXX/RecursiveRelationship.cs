using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[PrimaryKey(nameof(NodeId), nameof(MemberId))]
public class NodeMember
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid MemberId { get; set; }
    public Node Member { get; set; }
}

[PrimaryKey(nameof(NodeId), nameof(TypeId))]
public class NodeType
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid TypeId { get; set; }
    public Node Type { get; set; }
}

[PrimaryKey(nameof(NodeId), nameof(FromId))]
public class NodeDependency
{
    public Guid NodeId { get; set; }
    public Node Node { get; set; }

    public Guid FromId { get; set; }
    public Node From { get; set; }
}
