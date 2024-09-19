using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[Index(nameof(FilePath), nameof(StartLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine), nameof(Id), nameof(NodeId))]
[Index(nameof(Id), nameof(FilePath), nameof(StartLine))]
[Index(nameof(Id), nameof(NodeId))]
[Index(nameof(StartLine), nameof(EndLine), nameof(FilePath), nameof(Id), nameof(NodeId))]
[Index(nameof(EndLine), nameof(FilePath))]
[Index(nameof(NodeId), nameof(Id), nameof(FilePath), nameof(StartLine))]
public class DefineLocation : Location
{
}

[Index(nameof(FilePath), nameof(StartLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine), nameof(Id), nameof(NodeId))]
[Index(nameof(Id), nameof(FilePath), nameof(StartLine), nameof(EndLine), nameof(NodeId))]
[Index(nameof(Id), nameof(NodeId), nameof(FilePath), nameof(StartLine))]
[Index(nameof(StartLine), nameof(EndLine))]
[Index(nameof(EndLine), nameof(FilePath))]
public class ImplementationLocation : Location
{
}

public class Location
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public string FilePath { get; set; } = null!;
    [Required]
    public uint StartLine { get; set; }
    [Required]
    public uint EndLine { get; set; }

    public string? DisplayName { get; set; }
    public string? SourceCode { get; set; }

    [Required]
    public Guid NodeId { get; set; }
    public Node? Node { get; set; }

    public Location()
        => Id = Guid.NewGuid();
}
