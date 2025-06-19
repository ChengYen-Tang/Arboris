using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[Index(nameof(Id), nameof(ProjectId))]
[Index(nameof(Id), nameof(Spelling))]
[Index(nameof(ProjectId), nameof(Spelling), nameof(Id))]
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
    public string? IncludeStringsJson { get; set; }
    public string? AccessSpecifiers { get; set; }

    [Required]
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public DefineLocation? DefineLocation { get; set; }
    public ICollection<ImplementationLocation> ImplementationsLocation { get; set; }

    public ICollection<NodeMember> Members { get; set; }
    public ICollection<NodeType> Types { get; set; }
    public ICollection<NodeDependency> Dependencies { get; set; }

    private static JsonSerializerOptions jsonSerializerOptions { get; } = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    [NotMapped]
    public IReadOnlySet<string>? IncludeStrings
    {
        get => string.IsNullOrEmpty(IncludeStringsJson) ? null : JsonSerializer.Deserialize<HashSet<string>>(IncludeStringsJson, jsonSerializerOptions);
        set => IncludeStringsJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value, jsonSerializerOptions);
    }

    public Node()
        => (Id, Members, Types, Dependencies) = (Guid.NewGuid(), new List<NodeMember>(), new List<NodeType>(), new List<NodeDependency>());
}
