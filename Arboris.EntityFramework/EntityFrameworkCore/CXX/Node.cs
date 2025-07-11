using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[Index(nameof(ProjectId), nameof(VcProjectName),
       nameof(CursorKindSpelling), nameof(Spelling),
       nameof(CxType), nameof(NameSpace))]
public class Node
{
    [Key]
    public Guid Id { get; set; }
    public string VcProjectName { get; set; } = null!;
    public string? CursorKindSpelling { get; set; }
    public string? Spelling { get; set; }
    public string? CxType { get; set; }
    public string? NameSpace { get; set; }
    public string? LLMDescription { get; set; }
    public string? UserDescription { get; set; }
    public string? AccessSpecifiers { get; set; }

    [Required]
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public DefineLocation? DefineLocation { get; set; }
    public ICollection<ImplementationLocation> ImplementationsLocation { get; set; }

    public ICollection<NodeMember> Members { get; set; }
    public ICollection<NodeType> Types { get; set; }
    public ICollection<NodeDependency> Dependencies { get; set; }

    public Node()
        => (Id, Members, Types, Dependencies) = (Guid.NewGuid(), new List<NodeMember>(), new List<NodeType>(), new List<NodeDependency>());
}
