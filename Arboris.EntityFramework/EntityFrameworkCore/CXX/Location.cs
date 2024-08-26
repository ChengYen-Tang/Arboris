using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Arboris.EntityFramework.EntityFrameworkCore.CXX;

[Index(nameof(FilePath), nameof(StartLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine))]
public class DefineLocation : Location
{
}

[Index(nameof(FilePath), nameof(StartLine))]
[Index(nameof(FilePath), nameof(StartLine), nameof(EndLine))]
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
