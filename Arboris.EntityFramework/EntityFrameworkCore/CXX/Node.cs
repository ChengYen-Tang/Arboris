using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

public class Node
{
    [Key]
    public Guid Id { get; set; }
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }

    [Required]
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? HeaderLocationId { get; set; }
    public HeaderLocation? HeaderLocation { get; set; }
    public Guid? CppLocationId { get; set; }
    public CppLocation? CppLocation { get; set; }
    public Guid? HppLocationId { get; set; }
    public HppLocation? HppLocation { get; set; }

    public ICollection<NodeMember>? Members { get; set; }
    public ICollection<NodeType>? Types { get; set; }
    public ICollection<Dependency>? Dependencies { get; set; }

    public Node()
        => Id = Guid.NewGuid();
}
